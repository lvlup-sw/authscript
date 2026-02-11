namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for diagnosis (ICD-10).
/// </summary>
public sealed record DiagnosisModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
}
