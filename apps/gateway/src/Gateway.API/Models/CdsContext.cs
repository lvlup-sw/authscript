using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// Context data for CDS Hooks requests including user and patient information.
/// </summary>
public sealed record CdsContext
{
    /// <summary>
    /// Gets the FHIR ID of the current user (Practitioner resource).
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the FHIR ID of the patient in context.
    /// </summary>
    [JsonPropertyName("patientId")]
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the FHIR ID of the current encounter, if any.
    /// </summary>
    [JsonPropertyName("encounterId")]
    public string? EncounterId { get; init; }

    /// <summary>
    /// Gets the draft orders being evaluated for decision support.
    /// </summary>
    [JsonPropertyName("draftOrders")]
    public DraftOrders? DraftOrders { get; init; }
}
