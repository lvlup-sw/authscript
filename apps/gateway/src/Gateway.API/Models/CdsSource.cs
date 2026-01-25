using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// Source information for a CDS Hooks card identifying the decision support provider.
/// </summary>
public sealed record CdsSource
{
    /// <summary>
    /// Gets the short display label for the source (e.g., "AuthScript PA System").
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>
    /// Gets the optional URL to the source's website.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>
    /// Gets the optional URL to an icon image for the source.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }
}
