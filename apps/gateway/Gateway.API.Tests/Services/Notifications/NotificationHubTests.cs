using Gateway.API.Contracts;
using Gateway.API.Services.Notifications;

namespace Gateway.API.Tests.Services.Notifications;

/// <summary>
/// Tests for the NotificationHub service.
/// </summary>
public class NotificationHubTests
{
    [Test]
    public async Task NotificationHub_WriteAsync_WritesToChannel()
    {
        // Arrange
        var hub = new NotificationHub();
        var notification = new Notification(
            Type: "analysis_complete",
            TransactionId: "txn-123",
            EncounterId: "enc-456",
            PatientId: "patient-789",
            Message: "Analysis completed successfully");

        // Act
        await hub.WriteAsync(notification, CancellationToken.None);

        // Assert - Read back to verify it was written
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var readNotifications = new List<Notification>();

        try
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                readNotifications.Add(n);
                break; // Just get the first one
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if channel is empty
        }

        await Assert.That(readNotifications.Count).IsEqualTo(1);
        await Assert.That(readNotifications[0].TransactionId).IsEqualTo("txn-123");
    }

    [Test]
    public async Task NotificationHub_ReadAllAsync_ReturnsNotifications()
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

        await hub.WriteAsync(notification1, CancellationToken.None);
        await hub.WriteAsync(notification2, CancellationToken.None);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var readNotifications = new List<Notification>();

        try
        {
            await foreach (var n in hub.ReadAllAsync(cts.Token))
            {
                readNotifications.Add(n);
                if (readNotifications.Count >= 2) break;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if we read all available
        }

        // Assert
        await Assert.That(readNotifications.Count).IsEqualTo(2);
        await Assert.That(readNotifications[0].TransactionId).IsEqualTo("txn-001");
        await Assert.That(readNotifications[1].TransactionId).IsEqualTo("txn-002");
    }

    [Test]
    public async Task NotificationHub_Channel_IsUnbounded()
    {
        // Arrange
        var hub = new NotificationHub();
        const int notificationCount = 1000;

        // Act - Write many notifications without blocking
        for (var i = 0; i < notificationCount; i++)
        {
            await hub.WriteAsync(new Notification(
                Type: "test",
                TransactionId: $"txn-{i}",
                EncounterId: $"enc-{i}",
                PatientId: $"patient-{i}",
                Message: $"Test message {i}"), CancellationToken.None);
        }

        // Assert - Read back to verify all were written
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var readCount = 0;

        try
        {
            await foreach (var _ in hub.ReadAllAsync(cts.Token))
            {
                readCount++;
                if (readCount >= notificationCount) break;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout
        }

        await Assert.That(readCount).IsEqualTo(notificationCount);
    }
}
