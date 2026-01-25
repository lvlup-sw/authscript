using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// A suggested action group that the user can accept from a CDS card.
/// </summary>
public sealed record CdsSuggestion
{
    /// <summary>
    /// Gets the human-readable label for this suggestion.
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>
    /// Gets the unique identifier for this suggestion.
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    /// <summary>
    /// Gets whether this suggestion is the recommended choice.
    /// </summary>
    [JsonPropertyName("isRecommended")]
    public bool? IsRecommended { get; init; }

    /// <summary>
    /// Gets the list of FHIR actions to execute when this suggestion is accepted.
    /// </summary>
    [JsonPropertyName("actions")]
    public List<CdsAction>? Actions { get; init; }
}
