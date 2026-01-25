using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// Supporting evidence for a prior authorization criterion from clinical data.
/// </summary>
public sealed record EvidenceItem
{
    /// <summary>
    /// Gets the identifier for the criterion this evidence supports.
    /// </summary>
    [JsonPropertyName("criterion_id")]
    public required string CriterionId { get; init; }

    /// <summary>
    /// Gets the evaluation status: "met", "not_met", or "insufficient_data".
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets the clinical evidence text supporting the criterion.
    /// </summary>
    [JsonPropertyName("evidence")]
    public required string Evidence { get; init; }

    /// <summary>
    /// Gets the source of the evidence (e.g., "Condition", "Observation").
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Gets the confidence score for this evidence (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }
}
