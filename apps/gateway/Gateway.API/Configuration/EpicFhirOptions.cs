namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for Epic FHIR API connectivity.
/// </summary>
public sealed class EpicFhirOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Epic";

    /// <summary>
    /// Base URL for Epic FHIR R4 API.
    /// </summary>
    public required string FhirBaseUrl { get; init; }

    /// <summary>
    /// OAuth client ID for Epic.
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
    /// Path to the private key file for JWT signing.
    /// </summary>
    public string? PrivateKeyPath { get; init; }

    /// <summary>
    /// Signing algorithm for JWT (RS256, RS384, RS512). Defaults to RS384.
    /// </summary>
    public string SigningAlgorithm { get; init; } = "RS384";
}
