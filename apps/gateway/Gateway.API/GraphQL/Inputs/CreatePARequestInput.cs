namespace Gateway.API.GraphQL.Inputs;

public sealed record CreatePARequestInput(
    PatientInput Patient,
    string ProcedureCode,
    string DiagnosisCode,
    string DiagnosisName,
    string? ProviderId
);
