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

    /// <inheritdoc />
    public Task WriteAsync(Notification notification, CancellationToken ct)
    {
        if (_subscribers.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var writes = new List<Task>();
        foreach (var channel in _subscribers.Values)
        {
            writes.Add(channel.Writer.WriteAsync(notification, ct).AsTask());
        }

        return Task.WhenAll(writes);
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
            _subscribers.TryRemove(id, out _);
        }
    }
}
