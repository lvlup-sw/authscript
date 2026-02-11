namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for PA request statistics.
/// </summary>
public sealed record PAStatsModel
{
    public required int Ready { get; init; }
    public required int Submitted { get; init; }
    public required int WaitingForInsurance { get; init; }
    public required int Attention { get; init; }
    public required int Total { get; init; }
}
