using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Models;
using StackExchange.Redis;

namespace Gateway.API.Services;

/// <summary>
/// Redis-based storage for completed analysis results.
/// Gracefully handles missing Redis connections by disabling storage.
/// </summary>
public sealed class AnalysisResultStore : IAnalysisResultStore
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<AnalysisResultStore> _logger;
    private readonly IConfiguration _configuration;

    private const string KeyPrefix = "authscript:analysis";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisResultStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="configuration">Configuration for storage settings.</param>
    /// <param name="redis">Optional Redis connection multiplexer.</param>
    public AnalysisResultStore(
        ILogger<AnalysisResultStore> logger,
        IConfiguration configuration,
        IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _configuration = configuration;
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<PAFormData?> GetCachedResponseAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (!IsStorageEnabled() || _redis is null)
            return null;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:response:{cacheKey}";
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Store miss for {Key}", key);
                return null;
            }

            _logger.LogDebug("Store hit for {Key}", key);
            return JsonSerializer.Deserialize<PAFormData>((string)value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Store read failed for {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetCachedResponseAsync(string cacheKey, PAFormData formData, CancellationToken cancellationToken = default)
    {
        if (!IsStorageEnabled() || _redis is null)
            return;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:response:{cacheKey}";
            var json = JsonSerializer.Serialize(formData);

            await db.StringSetAsync(key, json, DefaultTtl);
            _logger.LogDebug("Stored response for {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Store write failed for {CacheKey}", cacheKey);
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetCachedPdfAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (!IsStorageEnabled() || _redis is null)
            return null;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:pdf:{cacheKey}";
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("PDF store miss for {Key}", key);
                return null;
            }

            _logger.LogDebug("PDF store hit for {Key}", key);
            return (byte[]?)value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF store read failed for {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetCachedPdfAsync(string cacheKey, byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        if (!IsStorageEnabled() || _redis is null)
            return;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:pdf:{cacheKey}";

            await db.StringSetAsync(key, pdfBytes, DefaultTtl);
            _logger.LogDebug("Stored PDF for {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF store write failed for {CacheKey}", cacheKey);
        }
    }

    private bool IsStorageEnabled()
    {
        return _configuration.GetValue<bool>("Analysis:EnableResultStorage", true);
    }
}
