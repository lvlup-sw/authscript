// =============================================================================
// <copyright file="MigrationGateMiddleware.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Net;

namespace Gateway.API.Data;

/// <summary>
/// Middleware that returns 503 Service Unavailable while database migrations are in progress.
/// Always allows health check endpoints through so orchestrators can monitor readiness.
/// </summary>
public sealed class MigrationGateMiddleware
{
    private static readonly string[] AllowedPaths = ["/health", "/alive"];
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationGateMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public MigrationGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware. Returns 503 if migrations are not yet complete,
    /// unless the request is to an allowed path (health checks).
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!MigrationHealthCheck.IsReady && !IsAllowedPath(context.Request.Path))
        {
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.Headers["Retry-After"] = "5";
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Service is starting up. Database migrations in progress.", context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static bool IsAllowedPath(PathString path)
    {
        foreach (var allowed in AllowedPaths)
        {
            if (path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
