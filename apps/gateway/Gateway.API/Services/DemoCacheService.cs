using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Models;
using StackExchange.Redis;

namespace Gateway.API.Services;

/// <summary>
/// Redis-based caching service for demo mode.
/// Gracefully handles missing Redis connections by disabling caching.
/// </summary>
public sealed class DemoCacheService : IDemoCacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<DemoCacheService> _logger;
    private readonly IConfiguration _configuration;

    private const string KeyPrefix = "authscript:demo";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoCacheService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="configuration">Configuration for cache settings.</param>
    /// <param name="redis">Optional Redis connection multiplexer.</param>
    public DemoCacheService(
        ILogger<DemoCacheService> logger,
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
        if (!IsCachingEnabled() || _redis is null)
            return null;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:response:{cacheKey}";
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for {Key}", key);
            return JsonSerializer.Deserialize<PAFormData>((string)value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetCachedResponseAsync(string cacheKey, PAFormData formData, CancellationToken cancellationToken = default)
    {
        if (!IsCachingEnabled() || _redis is null)
            return;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:response:{cacheKey}";
            var json = JsonSerializer.Serialize(formData);

            await db.StringSetAsync(key, json, DefaultTtl);
            _logger.LogDebug("Cached response for {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for {CacheKey}", cacheKey);
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetCachedPdfAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (!IsCachingEnabled() || _redis is null)
            return null;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:pdf:{cacheKey}";
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("PDF cache miss for {Key}", key);
                return null;
            }

            _logger.LogDebug("PDF cache hit for {Key}", key);
            return (byte[]?)value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF cache read failed for {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetCachedPdfAsync(string cacheKey, byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        if (!IsCachingEnabled() || _redis is null)
            return;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}:pdf:{cacheKey}";

            await db.StringSetAsync(key, pdfBytes, DefaultTtl);
            _logger.LogDebug("Cached PDF for {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF cache write failed for {CacheKey}", cacheKey);
        }
    }

    private bool IsCachingEnabled()
    {
        return _configuration.GetValue<bool>("Demo:EnableCaching", true);
    }
}
