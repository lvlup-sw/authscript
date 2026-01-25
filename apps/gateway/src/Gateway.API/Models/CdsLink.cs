using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// A link to an external resource or SMART app from a CDS card.
/// </summary>
public sealed record CdsLink
{
    /// <summary>
    /// Gets the human-readable label for this link.
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>
    /// Gets the URL to navigate to when the link is clicked.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Gets the link type: "absolute" for external URLs, "smart" for SMART app launches.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets the SMART app launch context data for "smart" type links.
    /// </summary>
    [JsonPropertyName("appContext")]
    public string? AppContext { get; init; }
}
