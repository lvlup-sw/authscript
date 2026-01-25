using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// FHIR Coding element representing a code from a terminology system.
/// </summary>
public sealed record Coding
{
    /// <summary>
    /// Gets the URI identifying the terminology system (e.g., SNOMED CT, ICD-10).
    /// </summary>
    [JsonPropertyName("system")]
    public string? System { get; init; }

    /// <summary>
    /// Gets the code value from the terminology system.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>
    /// Gets the human-readable display text for the code.
    /// </summary>
    [JsonPropertyName("display")]
    public string? Display { get; init; }
}
