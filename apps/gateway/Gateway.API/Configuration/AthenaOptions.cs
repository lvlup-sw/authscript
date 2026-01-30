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
    /// Token endpoint for OAuth 2.0 client credentials flow.
    /// </summary>
    public required string TokenEndpoint { get; init; }

    /// <summary>
    /// Polling interval for encounter detection, in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// Practice ID for athenahealth API requests.
    /// </summary>
    public string? PracticeId { get; init; }

    /// <summary>
    /// Validates that all required configuration properties are present.
    /// </summary>
    /// <returns>True if ClientId, FhirBaseUrl, and TokenEndpoint are non-empty; otherwise false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(FhirBaseUrl)
            && !string.IsNullOrWhiteSpace(TokenEndpoint);
    }
}
