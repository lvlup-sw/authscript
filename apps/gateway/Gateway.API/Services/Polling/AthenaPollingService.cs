using System.Text.Json;
using System.Threading.Channels;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.API.Services.Polling;

/// <summary>
/// Background service that polls athenahealth for finished encounters.
/// Token management is handled internally by FhirHttpClient via IFhirTokenProvider.
/// </summary>
public sealed class AthenaPollingService : BackgroundService, IEncounterPollingService
{
    private static readonly TimeSpan DefaultPurgeAge = TimeSpan.FromHours(24);

    private readonly IFhirHttpClient _fhirClient;
    private readonly AthenaOptions _options;
    private readonly ILogger<AthenaPollingService> _logger;
    private readonly IPatientRegistry _patientRegistry;
    private readonly Dictionary<string, DateTimeOffset> _processedEncounters = new();
    private readonly object _lock = new();
    private readonly Channel<string> _encounterChannel;
    private DateTimeOffset _lastCheck = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastPurge = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="AthenaPollingService"/> class.
    /// </summary>
    /// <param name="fhirClient">The FHIR HTTP client for API calls.</param>
    /// <param name="options">Configuration options for athenahealth.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="patientRegistry">Registry for tracking patients awaiting encounter completion.</param>
    public AthenaPollingService(
        IFhirHttpClient fhirClient,
        IOptions<AthenaOptions> options,
        ILogger<AthenaPollingService> logger,
        IPatientRegistry patientRegistry)
    {
        _fhirClient = fhirClient;
        _options = options.Value;
        _logger = logger;
        _patientRegistry = patientRegistry;
        _encounterChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
    }

    /// <summary>
    /// Gets the channel reader for consuming detected encounter IDs.
    /// </summary>
    public ChannelReader<string> Encounters => _encounterChannel.Reader;

    /// <summary>
    /// Gets the count of processed encounters currently tracked.
    /// </summary>
    /// <returns>The number of processed encounters.</returns>
    public int GetProcessedEncounterCount()
    {
        lock (_lock)
        {
            return _processedEncounters.Count;
        }
    }

    /// <summary>
    /// Checks if an encounter has already been processed.
    /// </summary>
    /// <param name="encounterId">The encounter ID to check.</param>
    /// <returns>True if the encounter has been processed; otherwise, false.</returns>
    public bool IsEncounterProcessed(string encounterId)
    {
        lock (_lock)
        {
            return _processedEncounters.ContainsKey(encounterId);
        }
    }

    /// <summary>
    /// Purges processed encounters older than the specified age.
    /// </summary>
    /// <param name="maxAge">The maximum age of entries to keep.</param>
    public void PurgeProcessedEncountersOlderThan(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        lock (_lock)
        {
            var keysToRemove = _processedEncounters
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _processedEncounters.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Purged {Count} old encounter entries", keysToRemove.Count);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AthenaPollingService starting");

        // Guard against non-positive polling interval to prevent hot spin or ArgumentOutOfRangeException
        var intervalSeconds = _options.PollingIntervalSeconds;
        if (intervalSeconds <= 0)
        {
            _logger.LogWarning(
                "Invalid PollingIntervalSeconds {Value}, using 1 second minimum",
                intervalSeconds);
            intervalSeconds = 1;
        }

        var pollDelay = TimeSpan.FromSeconds(intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollForFinishedEncountersAsync(stoppingToken);
                PurgeOldEntriesIfNeeded();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling for encounters");
            }

            try
            {
                await Task.Delay(pollDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("AthenaPollingService stopped");
    }

    private void PurgeOldEntriesIfNeeded()
    {
        // Purge every hour
        if (DateTimeOffset.UtcNow - _lastPurge > TimeSpan.FromHours(1))
        {
            PurgeProcessedEncountersOlderThan(DefaultPurgeAge);
            _lastPurge = DateTimeOffset.UtcNow;
        }
    }

    private async Task PollForFinishedEncountersAsync(CancellationToken ct)
    {
        // Guard against missing PracticeId to fail fast
        if (string.IsNullOrWhiteSpace(_options.PracticeId))
        {
            _logger.LogError("Athena PracticeId is missing; polling skipped");
            return;
        }

        // Capture timestamp BEFORE the search to avoid missing encounters created during the request
        var pollStart = DateTimeOffset.UtcNow;
        var query = $"ah-practice={_options.PracticeId}&status=finished&date=gt{_lastCheck:O}";

        _logger.LogDebug("Polling for encounters with query: {Query}", query);

        Result<JsonElement> result;
        try
        {
            result = await _fhirClient.SearchAsync("Encounter", query, ct);
        }
        catch (TokenAcquisitionException ex)
        {
            // Token acquisition failed (transient) - will retry on next poll cycle
            _logger.LogWarning("Unable to acquire Athena access token for polling: {Message}", ex.Message);
            return;
        }

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to search encounters: {Error}", result.Error?.Message);
            return;
        }

        // Only update _lastCheck on success, using the pre-captured timestamp
        _lastCheck = pollStart;

        await ProcessEncounterBundleAsync(result.Value, ct);
    }

    private async Task ProcessEncounterBundleAsync(JsonElement bundle, CancellationToken ct)
    {
        if (!bundle.TryGetProperty("entry", out var entries))
        {
            return;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource))
            {
                continue;
            }

            if (!resource.TryGetProperty("id", out var idElement))
            {
                continue;
            }

            var encounterId = idElement.GetString();
            if (string.IsNullOrEmpty(encounterId))
            {
                continue;
            }

            // Deduplication: skip if already processed
            bool isNew;
            lock (_lock)
            {
                if (_processedEncounters.ContainsKey(encounterId))
                {
                    _logger.LogDebug("Skipping already processed encounter: {EncounterId}", encounterId);
                    continue;
                }

                _processedEncounters[encounterId] = DateTimeOffset.UtcNow;
                isNew = true;
            }

            if (isNew)
            {
                _logger.LogInformation("Found finished encounter: {EncounterId}", encounterId);
                await _encounterChannel.Writer.WriteAsync(encounterId, ct);
            }
        }
    }
}
