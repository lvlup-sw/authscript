namespace Gateway.API.Configuration;

/// <summary>
/// Configuration settings for API key authentication.
/// </summary>
public sealed class ApiKeySettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ApiKey";

    /// <summary>
    /// List of valid API keys.
    /// </summary>
    public List<string> ValidApiKeys { get; init; } = [];
}
