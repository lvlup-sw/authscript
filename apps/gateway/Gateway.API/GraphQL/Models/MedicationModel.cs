namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for medication data.
/// </summary>
public sealed record MedicationModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Dosage { get; init; }
    public required string Category { get; init; }
    public required bool RequiresPA { get; init; }
}
