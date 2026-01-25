using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// A CDS Hooks card representing a single piece of decision support to display.
/// </summary>
public sealed record CdsCard
{
    /// <summary>
    /// Gets the unique identifier for this card.
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    /// <summary>
    /// Gets the one-sentence summary of the card's recommendation.
    /// </summary>
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    /// <summary>
    /// Gets the optional detailed information as markdown.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>
    /// Gets the urgency/severity indicator: "info", "warning", or "critical".
    /// </summary>
    [JsonPropertyName("indicator")]
    public required string Indicator { get; init; }

    /// <summary>
    /// Gets the source of the decision support content.
    /// </summary>
    [JsonPropertyName("source")]
    public required CdsSource Source { get; init; }

    /// <summary>
    /// Gets the suggested actions the user can take.
    /// </summary>
    [JsonPropertyName("suggestions")]
    public List<CdsSuggestion>? Suggestions { get; init; }

    /// <summary>
    /// Gets links to external resources or SMART apps.
    /// </summary>
    [JsonPropertyName("links")]
    public List<CdsLink>? Links { get; init; }

    /// <summary>
    /// Gets the reasons a user can select when overriding this card.
    /// </summary>
    [JsonPropertyName("overrideReasons")]
    public List<CdsOverrideReason>? OverrideReasons { get; init; }
}
