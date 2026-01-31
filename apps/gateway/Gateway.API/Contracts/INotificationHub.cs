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