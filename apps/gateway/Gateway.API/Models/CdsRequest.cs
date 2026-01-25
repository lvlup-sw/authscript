using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// CDS Hooks request payload for the order-select hook.
/// Contains context, authorization, and prefetch data for clinical decision support.
/// </summary>
public sealed record CdsRequest
{
    /// <summary>
    /// Gets the unique identifier for this hook invocation.
    /// </summary>
    [JsonPropertyName("hookInstance")]
    public required string HookInstance { get; init; }

    /// <summary>
    /// Gets the name of the CDS hook being invoked (e.g., "order-select").
    /// </summary>
    [JsonPropertyName("hook")]
    public required string Hook { get; init; }

    /// <summary>
    /// Gets the base URL of the FHIR server for additional queries.
    /// </summary>
    [JsonPropertyName("fhirServer")]
    public string? FhirServer { get; init; }

    /// <summary>
    /// Gets the OAuth 2.0 authorization for FHIR API access.
    /// </summary>
    [JsonPropertyName("fhirAuthorization")]
    public FhirAuthorization? FhirAuthorization { get; init; }

    /// <summary>
    /// Gets the context data including patient and draft orders.
    /// </summary>
    [JsonPropertyName("context")]
    public required CdsContext Context { get; init; }

    /// <summary>
    /// Gets prefetched FHIR resources to reduce network calls.
    /// </summary>
    [JsonPropertyName("prefetch")]
    public CdsPrefetch? Prefetch { get; init; }
}
