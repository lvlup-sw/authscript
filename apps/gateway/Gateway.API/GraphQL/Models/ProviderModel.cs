namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for provider data.
/// </summary>
public sealed record ProviderModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Npi { get; init; }
    public required string Specialty { get; init; }
}
