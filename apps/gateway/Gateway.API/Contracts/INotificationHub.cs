namespace Gateway.API.Contracts;

/// <summary>
/// Hub for broadcasting notifications about PA form processing events.
/// Supports Server-Sent Events (SSE) for real-time client updates.
/// </summary>
public interface INotificationHub
{
    /// <summary>
    /// Writes a notification to the hub for broadcasting to subscribers.
    /// </summary>
    /// <param name="notification">The notification to broadcast.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WriteAsync(Notification notification, CancellationToken ct);

    /// <summary>
    /// Reads all notifications from the hub as an async stream.
    /// Clients subscribe to this for real-time updates.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of notifications.</returns>
    IAsyncEnumerable<Notification> ReadAllAsync(CancellationToken ct);
}

/// <summary>
/// Notification message for PA form processing events.
/// </summary>
/// <param name="Type">The notification type (e.g., "PA_FORM_READY", "PROCESSING_ERROR").</param>
/// <param name="TransactionId">Unique transaction identifier for correlation.</param>
/// <param name="EncounterId">The FHIR Encounter resource ID.</param>
/// <param name="PatientId">The FHIR Patient resource ID.</param>
/// <param name="Message">Human-readable notification message.</param>
public sealed record Notification(
    string Type,
    string TransactionId,
    string EncounterId,
    string PatientId,
    string Message
);
