namespace Gateway.API.Models;

/// <summary>
/// Request for manually triggering an analysis.
/// </summary>
public sealed record ManualAnalysisRequest
{
    /// <summary>
    /// Gets the FHIR Patient resource ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the procedure code (CPT) to analyze.
    /// </summary>
    public required string ProcedureCode { get; init; }

    /// <summary>
    /// Gets the optional FHIR Encounter resource ID.
    /// </summary>
    public string? EncounterId { get; init; }

    /// <summary>
    /// Gets the OAuth access token for Epic FHIR access.
    /// </summary>
    public required string AccessToken { get; init; }
}
