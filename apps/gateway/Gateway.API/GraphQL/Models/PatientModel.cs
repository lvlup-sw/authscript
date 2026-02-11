namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for patient data.
/// </summary>
public sealed record PatientModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Mrn { get; init; }
    public required string Dob { get; init; }
    public required string MemberId { get; init; }
    public required string Payer { get; init; }
    public required string Address { get; init; }
    public required string Phone { get; init; }
}
