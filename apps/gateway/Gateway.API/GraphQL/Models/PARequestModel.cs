namespace Gateway.API.GraphQL.Models;

/// <summary>
/// GraphQL model for prior authorization request.
/// </summary>
public sealed record PARequestModel
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required PatientModel Patient { get; init; }
    public required string ProcedureCode { get; init; }
    public required string ProcedureName { get; init; }
    public required string Diagnosis { get; init; }
    public required string DiagnosisCode { get; init; }
    public required string Payer { get; init; }
    public required string Provider { get; init; }
    public required string ProviderNpi { get; init; }
    public required string ServiceDate { get; init; }
    public required string PlaceOfService { get; init; }
    public required string ClinicalSummary { get; init; }
    public required string Status { get; init; }
    public required int Confidence { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
    /// <summary>When status became ready for review (after AI processing).</summary>
    public string? ReadyAt { get; init; }
    /// <summary>When user submitted the PA.</summary>
    public string? SubmittedAt { get; init; }
    /// <summary>Reason for denial, if denied.</summary>
    public string? DenialReason { get; init; }
    /// <summary>Total seconds user spent on the review page before submitting.</summary>
    public int ReviewTimeSeconds { get; init; }
    public required IReadOnlyList<CriterionModel> Criteria { get; init; }
}
