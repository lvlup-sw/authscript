namespace Gateway.API.Configuration;

/// <summary>
/// Configuration options for Epic FHIR integration.
/// </summary>
public sealed class EpicFhirOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Epic";

    /// <summary>
    /// Base URL for the Epic FHIR R4 API.
    /// </summary>
    public required string FhirBaseUrl { get; init; }

    /// <summary>
    /// OAuth client ID for authentication.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// OAuth client secret for authentication.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// OAuth token endpoint URL. If null, no authentication is performed.
    /// </summary>
    public string? TokenEndpoint { get; init; }
}
