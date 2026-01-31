namespace Gateway.API.Contracts;

/// <summary>
/// Represents a notification for SSE streaming.
/// </summary>
/// <param name="Type">The notification type (e.g., "PA_FORM_READY", "PROCESSING_ERROR").</param>
/// <param name="TransactionId">The transaction ID this notification relates to.</param>
/// <param name="EncounterId">The encounter ID for context.</param>
/// <param name="PatientId">The patient ID for context.</param>
/// <param name="Message">A human-readable message describing the notification.</param>
public sealed record Notification(
    string Type,
    string TransactionId,
    string EncounterId,
    string PatientId,
    string Message);
