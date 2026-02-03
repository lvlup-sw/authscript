using Gateway.API.Contracts;
using Gateway.API.Services.Notifications;

namespace Gateway.API.Tests.Services.Notifications;

/// <summary>
/// Tests for the NotificationHub service.
/// </summary>
public class NotificationHubTests
{
    [Test]
    public async Task WriteAsync_WithActiveSubscriber_DeliversNotification()
    {
        // Arrange
        var hub = new NotificationHub();
        var notification = new Notification(
            Type: "analysis_complete",
            TransactionId: "txn-123",
            EncounterId: "enc-456",
            PatientId: "patient-789",
            Message: "Analysis completed successfully");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readNotifications = new List<Notification>();

        // Act - Start reading first (subscriber must be active before write)
        var readTask = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                readNotifications.Add(n);
                break; // Just get the first one
            }
        }, cts.Token);

        // Wait for subscriber to register (poll instead of fixed delay)
        await WaitForSubscribersAsync(hub, expectedCount: 1, cts.Token);

        await hub.WriteAsync(notification, CancellationToken.None);

        try
        {
            await readTask;
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout
        }

        // Assert
        await Assert.That(readNotifications.Count).IsEqualTo(1);
        await Assert.That(readNotifications[0].TransactionId).IsEqualTo("txn-123");
    }

    [Test]
    public async Task WriteAsync_WithNoSubscribers_CompletesImmediately()
    {
        // Arrange
        var hub = new NotificationHub();
        var notification = new Notification(
            Type: "analysis_complete",
            TransactionId: "txn-123",
            EncounterId: "enc-456",
            PatientId: "patient-789",
            Message: "Analysis completed successfully");

        // Act & Assert - Should complete without error when no subscribers
        await hub.WriteAsync(notification, CancellationToken.None);

        // If we get here without exception, the test passes
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ReadAllAsync_WithMultipleNotifications_ReceivesAll()
    {
        // Arrange
        var hub = new NotificationHub();
        var notification1 = new Notification(
            Type: "analysis_started",
            TransactionId: "txn-001",
            EncounterId: "enc-001",
            PatientId: "patient-001",
            Message: "Analysis started");
        var notification2 = new Notification(
            Type: "analysis_complete",
            TransactionId: "txn-002",
            EncounterId: "enc-002",
            PatientId: "patient-002",
            Message: "Analysis completed");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readNotifications = new List<Notification>();

        // Act - Start reading first
        var readTask = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                readNotifications.Add(n);
                if (readNotifications.Count >= 2) break;
            }
        }, cts.Token);

        // Wait for subscriber to register (poll instead of fixed delay)
        await WaitForSubscribersAsync(hub, expectedCount: 1, cts.Token);

        await hub.WriteAsync(notification1, CancellationToken.None);
        await hub.WriteAsync(notification2, CancellationToken.None);

        try
        {
            await readTask;
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout
        }

        // Assert
        await Assert.That(readNotifications.Count).IsEqualTo(2);
        await Assert.That(readNotifications[0].TransactionId).IsEqualTo("txn-001");
        await Assert.That(readNotifications[1].TransactionId).IsEqualTo("txn-002");
    }

    [Test]
    public async Task WriteAsync_WithMultipleSubscribers_BroadcastsToAll()
    {
        // Arrange
        var hub = new NotificationHub();
        var notification = new Notification(
            Type: "broadcast_test",
            TransactionId: "txn-broadcast",
            EncounterId: "enc-broadcast",
            PatientId: "patient-broadcast",
            Message: "Broadcast message");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var subscriber1Notifications = new List<Notification>();
        var subscriber2Notifications = new List<Notification>();
        var subscriber3Notifications = new List<Notification>();

        // Act - Start multiple subscribers
        var readTask1 = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                subscriber1Notifications.Add(n);
                break;
            }
        }, cts.Token);

        var readTask2 = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                subscriber2Notifications.Add(n);
                break;
            }
        }, cts.Token);

        var readTask3 = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                subscriber3Notifications.Add(n);
                break;
            }
        }, cts.Token);

        // Wait for all subscribers to register (poll instead of fixed delay)
        await WaitForSubscribersAsync(hub, expectedCount: 3, cts.Token);

        // Write a single notification
        await hub.WriteAsync(notification, CancellationToken.None);

        try
        {
            await Task.WhenAll(readTask1, readTask2, readTask3);
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout
        }

        // Assert - All three subscribers should receive the same notification
        await Assert.That(subscriber1Notifications.Count).IsEqualTo(1);
        await Assert.That(subscriber2Notifications.Count).IsEqualTo(1);
        await Assert.That(subscriber3Notifications.Count).IsEqualTo(1);

        await Assert.That(subscriber1Notifications[0].TransactionId).IsEqualTo("txn-broadcast");
        await Assert.That(subscriber2Notifications[0].TransactionId).IsEqualTo("txn-broadcast");
        await Assert.That(subscriber3Notifications[0].TransactionId).IsEqualTo("txn-broadcast");
    }

    [Test]
    public async Task ReadAllAsync_WhenCancelled_CleansUpSubscription()
    {
        // Arrange
        var hub = new NotificationHub();
        using var cts = new CancellationTokenSource();

        var readStarted = new TaskCompletionSource<bool>();
        var readCompleted = false;

        // Act - Start reading and then cancel
        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in hub.ReadAllAsync(cts.Token))
                {
                    readStarted.SetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            finally
            {
                readCompleted = true;
            }
        });

        // Wait for subscriber to register
        using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await WaitForSubscribersAsync(hub, expectedCount: 1, waitCts.Token);

        // Cancel the subscription
        cts.Cancel();

        await readTask;

        // Assert - Read should have completed (cleaned up)
        await Assert.That(readCompleted).IsTrue();
    }

    [Test]
    public async Task Channel_IsUnbounded_HandlesHighVolume()
    {
        // Arrange
        var hub = new NotificationHub();
        const int notificationCount = 1000;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readCount = 0;

        // Act - Start subscriber first
        var readTask = Task.Run(async () =>
        {
            await foreach (var _ in hub.ReadAllAsync(cts.Token))
            {
                readCount++;
                if (readCount >= notificationCount) break;
            }
        }, cts.Token);

        // Wait for subscriber to register (poll instead of fixed delay)
        await WaitForSubscribersAsync(hub, expectedCount: 1, cts.Token);

        // Write many notifications
        for (var i = 0; i < notificationCount; i++)
        {
            await hub.WriteAsync(new Notification(
                Type: "test",
                TransactionId: $"txn-{i}",
                EncounterId: $"enc-{i}",
                PatientId: $"patient-{i}",
                Message: $"Test message {i}"), CancellationToken.None);
        }

        try
        {
            await readTask;
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout
        }

        // Assert
        await Assert.That(readCount).IsEqualTo(notificationCount);
    }

    [Test]
    public async Task WriteAsync_WithMultipleSubscribers_AllReceiveAllMessages()
    {
        // Arrange
        var hub = new NotificationHub();
        const int messageCount = 5;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var subscriber1Messages = new List<Notification>();
        var subscriber2Messages = new List<Notification>();

        // Act - Start two subscribers
        var readTask1 = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                subscriber1Messages.Add(n);
                if (subscriber1Messages.Count >= messageCount) break;
            }
        }, cts.Token);

        var readTask2 = Task.Run(async () =>
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                subscriber2Messages.Add(n);
                if (subscriber2Messages.Count >= messageCount) break;
            }
        }, cts.Token);

        // Wait for subscribers to register (poll instead of fixed delay)
        await WaitForSubscribersAsync(hub, expectedCount: 2, cts.Token);

        // Write multiple messages
        for (var i = 0; i < messageCount; i++)
        {
            await hub.WriteAsync(new Notification(
                Type: "multi_test",
                TransactionId: $"txn-multi-{i}",
                EncounterId: $"enc-multi-{i}",
                PatientId: $"patient-multi-{i}",
                Message: $"Multi message {i}"), CancellationToken.None);
        }

        try
        {
            await Task.WhenAll(readTask1, readTask2);
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout
        }

        // Assert - Both subscribers should receive all messages
        await Assert.That(subscriber1Messages.Count).IsEqualTo(messageCount);
        await Assert.That(subscriber2Messages.Count).IsEqualTo(messageCount);

        // Verify message order is preserved for each subscriber
        for (var i = 0; i < messageCount; i++)
        {
            await Assert.That(subscriber1Messages[i].TransactionId).IsEqualTo($"txn-multi-{i}");
            await Assert.That(subscriber2Messages[i].TransactionId).IsEqualTo($"txn-multi-{i}");
        }
    }

    /// <summary>
    /// Helper to reliably wait for subscribers to register instead of using fixed delays.
    /// Throws TimeoutException if expected count is not reached before cancellation.
    /// </summary>
    private static async Task WaitForSubscribersAsync(NotificationHub hub, int expectedCount, CancellationToken ct)
    {
        while (hub.SubscriberCount < expectedCount && !ct.IsCancellationRequested)
        {
            await Task.Delay(10, ct);
        }

        if (hub.SubscriberCount < expectedCount)
        {
            throw new TimeoutException(
                $"Expected {expectedCount} subscriber(s) but only {hub.SubscriberCount} registered before timeout.");
        }
    }
}
