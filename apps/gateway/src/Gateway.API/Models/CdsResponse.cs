using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// CDS Hooks response containing decision support cards to display in the EHR.
/// </summary>
public sealed record CdsResponse
{
    /// <summary>
    /// Gets the collection of cards to display for clinical decision support.
    /// </summary>
    [JsonPropertyName("cards")]
    public required List<CdsCard> Cards { get; init; }
}
