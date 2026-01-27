using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Gateway.API.Services.Decorators;

/// <summary>
/// Decorator that adds HybridCache caching to the Intelligence client.
/// Uses a two-tier cache (L1 in-memory + L2 Redis) for optimal performance.
/// </summary>
public sealed class CachingIntelligenceClient : IIntelligenceClient
{
    private readonly IIntelligenceClient _inner;
    private readonly HybridCache _cache;
    private readonly CachingSettings _settings;
    private readonly ILogger<CachingIntelligenceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingIntelligenceClient"/> class.
    /// </summary>
    /// <param name="inner">The inner intelligence client to decorate.</param>
    /// <param name="cache">The hybrid cache instance.</param>
    /// <param name="settings">Caching configuration settings.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public CachingIntelligenceClient(
        IIntelligenceClient inner,
        HybridCache cache,
        IOptions<CachingSettings> settings,
        ILogger<CachingIntelligenceClient> logger)
    {
        _inner = inner;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PAFormData> AnalyzeAsync(
        ClinicalBundle clinicalBundle,
        string procedureCode,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(clinicalBundle.PatientId, procedureCode);

        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogDebug("Cache miss for {CacheKey}, calling intelligence service", cacheKey);
                return await _inner.AnalyzeAsync(clinicalBundle, procedureCode, ct);
            },
            new HybridCacheEntryOptions
            {
                Expiration = _settings.Duration,
                LocalCacheExpiration = _settings.LocalCacheDuration
            },
            cancellationToken: cancellationToken);

        _logger.LogDebug("Analysis result retrieved for {CacheKey}", cacheKey);
        return result;
    }

    private string BuildCacheKey(string patientId, string procedureCode)
    {
        return $"{_settings.KeyPrefix}:analysis:{patientId}:{procedureCode}";
    }
}
