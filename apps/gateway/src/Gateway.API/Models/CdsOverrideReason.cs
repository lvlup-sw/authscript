using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// A reason code that users can select when overriding a CDS card recommendation.
/// </summary>
public sealed record CdsOverrideReason
{
    /// <summary>
    /// Gets the code identifier for this override reason.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable display text for this override reason.
    /// </summary>
    [JsonPropertyName("display")]
    public required string Display { get; init; }
}
