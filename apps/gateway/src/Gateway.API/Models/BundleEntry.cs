using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// Entry within a FHIR Bundle containing a ServiceRequest resource.
/// </summary>
public sealed record BundleEntry
{
    /// <summary>
    /// Gets the ServiceRequest resource for this entry.
    /// </summary>
    [JsonPropertyName("resource")]
    public ServiceRequestResource? Resource { get; init; }
}
