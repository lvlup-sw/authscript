namespace Gateway.API.GraphQL.Inputs;

/// <summary>
/// Patient data from frontend (Athena test patients). Not stored on backend.
/// </summary>
public sealed record PatientInput(
    string Id,
    string PatientId,
    string? FhirId,
    string Name,
    string Mrn,
    string Dob,
    string MemberId,
    string Payer,
    string Address,
    string Phone
);
