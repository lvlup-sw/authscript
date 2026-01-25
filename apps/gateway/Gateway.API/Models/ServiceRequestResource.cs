using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// FHIR ServiceRequest resource representing an order for a service or procedure.
/// </summary>
public sealed record ServiceRequestResource
{
    /// <summary>
    /// Gets the FHIR resource type, always "ServiceRequest".
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; init; } = "ServiceRequest";

    /// <summary>
    /// Gets the logical ID of this resource.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the code describing what is being requested (procedure/service).
    /// </summary>
    [JsonPropertyName("code")]
    public CodeableConcept? Code { get; init; }
}
