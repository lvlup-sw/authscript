namespace Gateway.API.Contracts;

/// <summary>
/// Represents a notification for SSE streaming.
/// </summary>
/// <param name="Type">The notification type (e.g., "PA_FORM_READY", "PROCESSING_ERROR", "WORK_ITEM_STATUS_CHANGED").</param>
/// <param name="TransactionId">The transaction ID this notification relates to.</param>
/// <param name="EncounterId">The encounter ID for context.</param>
/// <param name="PatientId">The patient ID for context.</param>
/// <param name="Message">A human-readable message describing the notification.</param>
/// <param name="WorkItemId">The work item ID (for WORK_ITEM_STATUS_CHANGED notifications).</param>
/// <param name="NewStatus">The new work item status (for WORK_ITEM_STATUS_CHANGED notifications).</param>
/// <param name="ServiceRequestId">The FHIR ServiceRequest ID if available (for WORK_ITEM_STATUS_CHANGED notifications).</param>
/// <param name="ProcedureCode">The procedure code if available (for WORK_ITEM_STATUS_CHANGED notifications).</param>
public sealed record Notification(
    string Type,
    string TransactionId,
    string EncounterId,
    string PatientId,
    string Message,
    string? WorkItemId = null,
    string? NewStatus = null,
    string? ServiceRequestId = null,
    string? ProcedureCode = null);
