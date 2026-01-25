using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// A FHIR action to be performed when a CDS suggestion is accepted.
/// </summary>
public sealed record CdsAction
{
    /// <summary>
    /// Gets the type of action: "create", "update", or "delete".
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets the human-readable description of this action.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the FHIR resource to create, update, or delete.
    /// </summary>
    [JsonPropertyName("resource")]
    public object? Resource { get; init; }
}
