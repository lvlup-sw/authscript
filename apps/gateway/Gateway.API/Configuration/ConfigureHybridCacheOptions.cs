using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Gateway.API.Configuration;

/// <summary>
/// Configures HybridCache options using CachingSettings.
/// </summary>
public sealed class ConfigureHybridCacheOptions : IConfigureOptions<HybridCacheOptions>
{
    private readonly CachingSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureHybridCacheOptions"/> class.
    /// </summary>
    /// <param name="settings">The caching settings.</param>
    public ConfigureHybridCacheOptions(IOptions<CachingSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public void Configure(HybridCacheOptions options)
    {
        options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = _settings.Duration,
            LocalCacheExpiration = _settings.LocalCacheDuration
        };
    }
}
