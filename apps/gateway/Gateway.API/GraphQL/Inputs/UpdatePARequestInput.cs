namespace Gateway.API.GraphQL.Inputs;

public sealed record UpdatePARequestInput(
    string Id,
    string? Diagnosis,
    string? DiagnosisCode,
    string? ServiceDate,
    string? PlaceOfService,
    string? ClinicalSummary,
    IReadOnlyList<CriterionInput>? Criteria
);

public sealed record CriterionInput(bool? Met, string Label);
