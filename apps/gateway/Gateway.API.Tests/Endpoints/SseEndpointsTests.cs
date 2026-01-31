using Gateway.API.Contracts;
using Gateway.API.Endpoints;
using Gateway.API.Services.Notifications;
using Microsoft.AspNetCore.Http;

namespace Gateway.API.Tests.Endpoints;

/// <summary>
/// Tests for SSE streaming endpoints.
/// </summary>
public class SseEndpointsTests
{
    [Test]
    public async Task SseEndpoint_Get_ReturnsTextEventStreamContentType()
    {
        // Arrange
        var hub = new NotificationHub();
        var context = CreateHttpContext();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Start streaming (subscriber must be active before write)
        var streamTask = SseEndpoints.StreamEventsAsync(context, hub, cts.Token);

        // Give subscriber time to register
        await Task.Delay(50);

        // Write a notification so endpoint can process something
        await hub.WriteAsync(new Notification(
            Type: "test",
            TransactionId: "txn-1",
            EncounterId: "enc-1",
            PatientId: "patient-1",
            Message: "Test"), CancellationToken.None);

        // Wait for streaming to complete
        await streamTask;

        // Assert
        await Assert.That(context.Response.ContentType).IsEqualTo("text/event-stream");
    }

    [Test]
    public async Task SseEndpoint_Get_SetsCacheControlNoCache()
    {
        // Arrange
        var hub = new NotificationHub();
        var context = CreateHttpContext();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Start streaming (subscriber must be active before write)
        var streamTask = SseEndpoints.StreamEventsAsync(context, hub, cts.Token);

        // Give subscriber time to register
        await Task.Delay(50);

        // Write a notification
        await hub.WriteAsync(new Notification(
            Type: "test",
            TransactionId: "txn-1",
            EncounterId: "enc-1",
            PatientId: "patient-1",
            Message: "Test"), CancellationToken.None);

        // Wait for streaming to complete
        await streamTask;

        // Assert
        await Assert.That(context.Response.Headers.CacheControl.ToString()).IsEqualTo("no-cache");
    }

    [Test]
    public async Task SseEndpoint_Get_StreamsNotifications()
    {
        // Arrange
        var hub = new NotificationHub();
        var context = CreateHttpContext();
        var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        var notification = new Notification(
            Type: "analysis_complete",
            TransactionId: "txn-123",
            EncounterId: "enc-456",
            PatientId: "patient-789",
            Message: "Analysis completed");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Start streaming (subscriber must be active before write)
        var streamTask = SseEndpoints.StreamEventsAsync(context, hub, cts.Token);

        // Give subscriber time to register
        await Task.Delay(50);

        // Write notification after subscriber is registered
        await hub.WriteAsync(notification, CancellationToken.None);

        // Wait for streaming to complete (or timeout)
        await streamTask;

        // Assert - Check the response body
        memoryStream.Seek(0, SeekOrigin.Begin);
        var streamedContent = new StreamReader(memoryStream).ReadToEnd();

        await Assert.That(streamedContent).Contains("data:");
        await Assert.That(streamedContent).Contains("txn-123");
        await Assert.That(streamedContent).Contains("analysis_complete");
    }

    [Test]
    public async Task SseEndpoint_Get_SetsConnectionKeepAlive()
    {
        // Arrange
        var hub = new NotificationHub();
        var context = CreateHttpContext();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Start streaming (subscriber must be active before write)
        var streamTask = SseEndpoints.StreamEventsAsync(context, hub, cts.Token);

        // Give subscriber time to register
        await Task.Delay(50);

        // Write a notification
        await hub.WriteAsync(new Notification(
            Type: "test",
            TransactionId: "txn-1",
            EncounterId: "enc-1",
            PatientId: "patient-1",
            Message: "Test"), CancellationToken.None);

        // Wait for streaming to complete
        await streamTask;

        // Assert
        await Assert.That(context.Response.Headers.Connection.ToString()).IsEqualTo("keep-alive");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
