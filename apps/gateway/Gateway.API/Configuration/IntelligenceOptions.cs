namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for Intelligence service connectivity.
/// </summary>
public sealed class IntelligenceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Intelligence";

    /// <summary>
    /// Base URL for Intelligence API.
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
}
