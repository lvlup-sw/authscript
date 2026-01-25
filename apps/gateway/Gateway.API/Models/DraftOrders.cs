using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// FHIR Bundle containing draft ServiceRequest resources for order-select hook.
/// </summary>
public sealed record DraftOrders
{
    /// <summary>
    /// Gets the FHIR resource type, always "Bundle".
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; init; } = "Bundle";

    /// <summary>
    /// Gets the bundle entries containing draft orders.
    /// </summary>
    [JsonPropertyName("entry")]
    public List<BundleEntry>? Entry { get; init; }
}
