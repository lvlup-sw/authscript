namespace Gateway.API.Models;

/// <summary>
/// Represents a prior authorization work item that tracks the lifecycle of a PA request.
/// </summary>
public sealed record WorkItem
{
    /// <summary>
    /// Unique identifier for the work item.
    /// </summary>
    public required string Id { get; init; }

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
    /// </summary>
    public required string ServiceRequestId { get; init; }

    /// <summary>
    /// Current status of the work item in its lifecycle.
    /// </summary>
    public required WorkItemStatus Status { get; init; }

    /// <summary>
    /// CPT code for the procedure requiring prior authorization.
    /// </summary>
    public required string ProcedureCode { get; init; }

    /// <summary>
    /// Timestamp when the work item was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the work item was last updated. Null if never updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
