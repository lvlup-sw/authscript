namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for procedure data.
/// </summary>
public sealed record ProcedureModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required bool RequiresPA { get; init; }
}
