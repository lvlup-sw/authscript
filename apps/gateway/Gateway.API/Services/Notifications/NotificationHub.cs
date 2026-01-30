using System.Threading.Channels;
using Gateway.API.Contracts;

namespace Gateway.API.Services.Notifications;

/// <summary>
/// Channel-based notification hub for SSE streaming.
/// Provides an unbounded channel for publishing notifications to all subscribers.
/// </summary>
public sealed class NotificationHub : INotificationHub
{
    private readonly Channel<Notification> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHub"/> class.
    /// Creates an unbounded channel for maximum throughput.
    /// </summary>
    public NotificationHub()
    {
        _channel = Channel.CreateUnbounded<Notification>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    /// <inheritdoc />
    public async Task WriteAsync(Notification notification, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(notification, ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Notification> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
