namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for recent activity item.
/// </summary>
public sealed record ActivityItemModel
{
    public required string Id { get; init; }
    public required string Action { get; init; }
    public required string PatientName { get; init; }
    public required string ProcedureCode { get; init; }
    public required string Time { get; init; }
    public required string Type { get; init; }
}
