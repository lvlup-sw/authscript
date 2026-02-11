namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for policy criterion in a PA request.
/// </summary>
public sealed record CriterionModel
{
    public bool? Met { get; init; }
    public required string Label { get; init; }
}
