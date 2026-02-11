namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for payer data.
/// </summary>
public sealed record PayerModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Phone { get; init; }
    public required string FaxNumber { get; init; }
}
