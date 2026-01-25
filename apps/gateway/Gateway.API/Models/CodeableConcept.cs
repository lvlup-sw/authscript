using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// FHIR CodeableConcept representing a coded value with optional text.
/// </summary>
public sealed record CodeableConcept
{
    /// <summary>
    /// Gets the list of coding references for this concept.
    /// </summary>
    [JsonPropertyName("coding")]
    public List<Coding>? Coding { get; init; }

    /// <summary>
    /// Gets the plain text representation of the concept.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
