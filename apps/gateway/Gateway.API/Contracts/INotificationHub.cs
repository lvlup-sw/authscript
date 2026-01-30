namespace Gateway.API.Contracts;

/// <summary>
/// Interface for publishing and subscribing to real-time notifications.
/// Uses a channel-based approach for SSE streaming.
/// </summary>
public interface INotificationHub
{
    /// <summary>
    /// Writes a notification to the channel for all subscribers.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WriteAsync(Notification notification, CancellationToken ct);

    /// <summary>
    /// Returns an async enumerable of all notifications.
    /// This will block until notifications are available or cancellation is requested.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of notifications.</returns>
    IAsyncEnumerable<Notification> ReadAllAsync(CancellationToken ct);
}

/// <summary>
/// Represents a notification for SSE streaming.
/// </summary>
/// <param name="Type">The notification type (e.g., "analysis_started", "analysis_complete").</param>
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
