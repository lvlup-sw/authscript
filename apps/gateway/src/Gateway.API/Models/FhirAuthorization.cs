using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// OAuth 2.0 authorization information for FHIR server access.
/// </summary>
public sealed record FhirAuthorization
{
    /// <summary>
    /// Gets the OAuth access token for FHIR API calls.
    /// </summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the token type, typically "Bearer".
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Gets the number of seconds until the token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Gets the OAuth scopes granted for this token.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the subject identifier (typically the user or system).
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }
}
