using System.Text.Json;
using System.Threading.Channels;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Exceptions;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<string, DateTimeOffset> _processedEncounters = new();
    private readonly object _lock = new();
    private readonly Channel<EncounterCompletedEvent> _encounterChannel;
    private DateTimeOffset _lastCheck = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastPurge = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="AthenaPollingService"/> class.
    /// </summary>
    /// <param name="fhirClient">The FHIR HTTP client for API calls.</param>
    /// <param name="options">Configuration options for athenahealth.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="scopeFactory">Factory for creating scoped service instances.</param>
    public AthenaPollingService(
        IFhirHttpClient fhirClient,
        IOptions<AthenaOptions> options,
        ILogger<AthenaPollingService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _fhirClient = fhirClient;
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _encounterChannel = Channel.CreateUnbounded<EncounterCompletedEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Gets the channel reader for consuming encounter completion events.
    /// </summary>
    public ChannelReader<EncounterCompletedEvent> Encounters => _encounterChannel.Reader;

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

        // Create scope for scoped services (IPatientRegistry uses scoped DbContext)
        using var scope = _scopeFactory.CreateScope();
        var patientRegistry = scope.ServiceProvider.GetRequiredService<IPatientRegistry>();

        // Get registered patients to poll
        var patients = await patientRegistry.GetActiveAsync(ct).ConfigureAwait(false);

        if (patients.Count == 0)
        {
            _logger.LogDebug("No registered patients to poll");
            return;
        }

        _logger.LogDebug("Polling {Count} registered patients", patients.Count);

        // Poll each patient in parallel (max 5 concurrent)
        await Parallel.ForEachAsync(
            patients,
            new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct },
            async (patient, token) =>
            {
                await PollPatientEncounterAsync(patient, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls a specific patient's encounter for status changes.
    /// </summary>
    /// <param name="patient">The registered patient to poll.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PollPatientEncounterAsync(
        RegisteredPatient patient,
        CancellationToken ct)
    {
        try
        {
            // Skip if already processed
            if (IsEncounterProcessed(patient.EncounterId))
            {
                _logger.LogDebug("Skipping already processed encounter {EncounterId}", patient.EncounterId);
                return;
            }

            // Build per-patient query using AthenaQueryBuilder
            var query = AthenaQueryBuilder.BuildEncounterQuery(
                patient.PatientId,
                patient.EncounterId,
                patient.PracticeId);

            _logger.LogDebug("Polling encounter for patient {PatientId}: {Query}", patient.PatientId, query);

            var result = await _fhirClient.SearchAsync("Encounter", query, ct).ConfigureAwait(false);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to poll encounter for patient {PatientId}: {Error}",
                    patient.PatientId,
                    result.Error?.Message);
                return;
            }

            // Extract status from FHIR response
            var status = ExtractEncounterStatus(result.Value);
            if (status is null)
            {
                _logger.LogWarning("Could not extract status for patient {PatientId}", patient.PatientId);
                return;
            }

            // Check for status transition to "finished"
            if (status == "finished" && patient.CurrentEncounterStatus != "finished")
            {
                // Mark encounter as processed to prevent duplicate emissions
                lock (_lock)
                {
                    _processedEncounters[patient.EncounterId] = DateTimeOffset.UtcNow;
                }

                _logger.LogInformation("Encounter completed for patient {PatientId}", patient.PatientId);

                // Emit full event to channel
                var evt = new EncounterCompletedEvent
                {
                    PatientId = patient.PatientId,
                    EncounterId = patient.EncounterId,
                    PracticeId = patient.PracticeId,
                    WorkItemId = patient.WorkItemId,
                };
                await _encounterChannel.Writer.WriteAsync(evt, ct).ConfigureAwait(false);

                // Auto-unregister patient using scoped service
                using var scope = _scopeFactory.CreateScope();
                var patientRegistry = scope.ServiceProvider.GetRequiredService<IPatientRegistry>();
                await patientRegistry.UnregisterAsync(patient.PatientId, ct).ConfigureAwait(false);
            }
            else
            {
                // Update registry with poll timestamp and status using scoped service
                using var scope = _scopeFactory.CreateScope();
                var patientRegistry = scope.ServiceProvider.GetRequiredService<IPatientRegistry>();
                await patientRegistry.UpdateAsync(
                    patient.PatientId,
                    DateTimeOffset.UtcNow,
                    status,
                    ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling encounter for patient {PatientId}", patient.PatientId);
        }
    }

    private static string? ExtractEncounterStatus(JsonElement bundle)
    {
        if (!bundle.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
        {
            return null;
        }

        var firstEntry = entries[0];
        if (!firstEntry.TryGetProperty("resource", out var resource))
        {
            return null;
        }

        if (!resource.TryGetProperty("status", out var statusElement))
        {
            return null;
        }

        return statusElement.GetString();
    }

}
