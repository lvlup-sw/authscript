using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Gateway.API.Contracts;

namespace Gateway.API.Services.Notifications;

/// <summary>
/// Channel-based notification hub for SSE streaming with fan-out broadcast.
/// Each subscriber gets their own channel, ensuring all subscribers receive all notifications.
/// </summary>
public sealed class NotificationHub : INotificationHub
{
    private readonly ConcurrentDictionary<Guid, Channel<Notification>> _subscribers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHub"/> class.
    /// </summary>
    public NotificationHub()
    {
    }

    /// <summary>
    /// Gets the current number of active subscribers.
    /// Useful for testing to verify subscriber registration.
    /// </summary>
    public int SubscriberCount => _subscribers.Count;

    /// <inheritdoc />
    public Task WriteAsync(Notification notification, CancellationToken ct)
    {
        // Fan-out: broadcast to all subscribers using TryWrite for graceful handling of closed channels
        foreach (var channel in _subscribers.Values)
        {
            // TryWrite is synchronous and returns false for closed/full channels (unbounded never full)
            channel.Writer.TryWrite(notification);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Notification> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<Notification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var id = Guid.NewGuid();
        _subscribers[id] = channel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
            {
                yield return item;
            }
        }
        finally
        {
            if (_subscribers.TryRemove(id, out var removed))
            {
                removed.Writer.TryComplete();
            }
        }
    }
}
