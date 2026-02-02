namespace Gateway.API.Models;

/// <summary>
/// Request to create a new work item.
/// </summary>
public sealed record CreateWorkItemRequest
{
    /// <summary>
    /// FHIR Encounter ID that triggered this work item.
    /// </summary>
    public required string EncounterId { get; init; }

    /// <summary>
    /// FHIR Patient ID associated with this work item.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// FHIR ServiceRequest ID for the order requiring prior authorization.
    /// Optional; populated after analysis.
    /// </summary>
    public string? ServiceRequestId { get; init; }

    /// <summary>
    /// CPT code for the procedure requiring prior authorization.
    /// Optional; populated after analysis.
    /// </summary>
    public string? ProcedureCode { get; init; }

    /// <summary>
    /// Optional initial status. If not specified, downstream logic assigns MissingData.
    /// </summary>
    public WorkItemStatus? Status { get; init; }
}
