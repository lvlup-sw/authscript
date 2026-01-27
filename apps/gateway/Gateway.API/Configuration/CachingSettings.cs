namespace Gateway.API.Configuration;

/// <summary>
/// Configuration settings for caching behavior.
/// </summary>
public sealed class CachingSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the cache duration.
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the local (L1) cache duration.
    /// </summary>
    public TimeSpan LocalCacheDuration { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the cache key prefix.
    /// </summary>
    public string KeyPrefix { get; init; } = "authscript";

    /// <summary>
    /// Validates the settings configuration.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid() => Duration > TimeSpan.Zero;
}
