// =============================================================================
// <copyright file="MigrationGateMiddlewareTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Tests for <see cref="MigrationGateMiddleware"/>.
/// </summary>
[NotInParallel("MigrationHealthCheck")]
public class MigrationGateMiddlewareTests
{
    [Before(Test)]
    public void Setup()
    {
        MigrationHealthCheck.Reset();
    }

    [After(Test)]
    public void Cleanup()
    {
        MigrationHealthCheck.Reset();
    }

    [Test]
    public async Task InvokeAsync_NotReady_Returns503()
    {
        // Arrange — register but don't complete
        MigrationHealthCheck.RegisterExpected("TestContext");
        var nextCalled = false;
        var middleware = new MigrationGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext("/api/graphql");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await Assert.That(context.Response.StatusCode).IsEqualTo(503);
        await Assert.That(nextCalled).IsFalse();
    }

    [Test]
    public async Task InvokeAsync_NotReady_SetsRetryAfterHeader()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        var middleware = new MigrationGateMiddleware(_ => Task.CompletedTask);
        var context = CreateHttpContext("/api/graphql");

        await middleware.InvokeAsync(context);

        await Assert.That(context.Response.Headers["Retry-After"].ToString()).IsEqualTo("5");
    }

    [Test]
    public async Task InvokeAsync_Ready_CallsNext()
    {
        // Arrange — complete migration
        MigrationHealthCheck.RegisterExpected("TestContext");
        MigrationHealthCheck.MarkComplete("TestContext");
        var nextCalled = false;
        var middleware = new MigrationGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext("/api/graphql");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await Assert.That(nextCalled).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_NotReady_HealthEndpoint_CallsNext()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        var nextCalled = false;
        var middleware = new MigrationGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext("/health");

        await middleware.InvokeAsync(context);

        await Assert.That(nextCalled).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_NotReady_AliveEndpoint_CallsNext()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        var nextCalled = false;
        var middleware = new MigrationGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext("/alive");

        await middleware.InvokeAsync(context);

        await Assert.That(nextCalled).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_NoRegistrations_Returns503()
    {
        // No registrations means IsReady is false (Count == 0)
        var nextCalled = false;
        var middleware = new MigrationGateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext("/api/data");

        await middleware.InvokeAsync(context);

        await Assert.That(context.Response.StatusCode).IsEqualTo(503);
        await Assert.That(nextCalled).IsFalse();
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
