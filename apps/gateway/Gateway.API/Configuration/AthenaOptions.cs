namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for athenahealth FHIR API connectivity and polling.
/// </summary>
public sealed class AthenaOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Athena";

    /// <summary>
    /// Base URL for athenahealth FHIR R4 API.
    /// </summary>
    public required string FhirBaseUrl { get; init; }

    /// <summary>
    /// OAuth client ID for athenahealth.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// OAuth client secret (from user-secrets in dev).
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Token endpoint for client credentials flow.
    /// </summary>
    public string? TokenEndpoint { get; init; }

    /// <summary>
    /// Polling interval in seconds for encounter detection.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 60;

    /// <summary>
    /// OAuth access token for polling (from auth service).
    /// </summary>
    public string? AccessToken { get; init; }
}
