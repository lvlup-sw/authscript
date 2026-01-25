namespace Gateway.API.Models;

/// <summary>
/// Aggregated clinical data from FHIR queries for prior authorization processing.
/// Contains patient demographics and clinical history.
/// </summary>
public sealed record ClinicalBundle
{
    /// <summary>
    /// Gets the FHIR Patient resource ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the patient demographic information.
    /// </summary>
    public PatientInfo? Patient { get; init; }

    /// <summary>
    /// Gets the list of active and historical conditions/diagnoses.
    /// </summary>
    public List<ConditionInfo> Conditions { get; init; } = [];

    /// <summary>
    /// Gets the list of clinical observations (lab results, vitals, etc.).
    /// </summary>
    public List<ObservationInfo> Observations { get; init; } = [];

    /// <summary>
    /// Gets the list of prior procedures.
    /// </summary>
    public List<ProcedureInfo> Procedures { get; init; } = [];

    /// <summary>
    /// Gets the list of clinical documents (notes, reports, etc.).
    /// </summary>
    public List<DocumentInfo> Documents { get; init; } = [];
}
