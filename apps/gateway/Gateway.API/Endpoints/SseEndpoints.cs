using System.Text.Json;
using Gateway.API.Contracts;

namespace Gateway.API.Endpoints;

/// <summary>
/// Endpoints for Server-Sent Events (SSE) streaming.
/// Provides real-time notifications to connected clients.
/// </summary>
public static class SseEndpoints
{
    /// <summary>
    /// Maps the SSE endpoints to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapSseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events", async (HttpContext ctx, INotificationHub hub, CancellationToken ct) =>
        {
            await StreamEventsAsync(ctx, hub, ct);
        })
        .WithName("StreamEvents")
        .WithTags("Events")
        .WithSummary("Stream real-time events via SSE")
        .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
    }

    /// <summary>
    /// Streams events to the client via SSE.
    /// </summary>
    /// <param name="ctx">The HTTP context.</param>
    /// <param name="hub">The notification hub.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task StreamEventsAsync(HttpContext ctx, INotificationHub hub, CancellationToken ct)
    {
        ctx.Response.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var notification in hub.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(notification);
                await ctx.Response.WriteAsync($"data: {json}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when client disconnects or timeout
        }
    }
}
