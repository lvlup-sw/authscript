using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// Prefetched FHIR resources provided by the CDS client to avoid additional queries.
/// </summary>
public sealed record CdsPrefetch
{
    /// <summary>
    /// Gets the prefetched Patient resource.
    /// </summary>
    [JsonPropertyName("patient")]
    public object? Patient { get; init; }

    /// <summary>
    /// Gets the prefetched ServiceRequest resource.
    /// </summary>
    [JsonPropertyName("serviceRequest")]
    public object? ServiceRequest { get; init; }
}
