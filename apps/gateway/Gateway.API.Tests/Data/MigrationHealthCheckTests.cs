// =============================================================================
// <copyright file="MigrationHealthCheckTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Tests for <see cref="MigrationHealthCheck"/>.
/// </summary>
[NotInParallel("MigrationHealthCheck")]
public class MigrationHealthCheckTests
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
    public async Task IsReady_NoRegistrations_ReturnsFalse()
    {
        await Assert.That(MigrationHealthCheck.IsReady).IsFalse();
    }

    [Test]
    public async Task IsReady_RegisteredButNotComplete_ReturnsFalse()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");

        await Assert.That(MigrationHealthCheck.IsReady).IsFalse();
    }

    [Test]
    public async Task IsReady_AllComplete_ReturnsTrue()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        MigrationHealthCheck.MarkComplete("TestContext");

        await Assert.That(MigrationHealthCheck.IsReady).IsTrue();
    }

    [Test]
    public async Task IsReady_MultipleContexts_OneIncomplete_ReturnsFalse()
    {
        MigrationHealthCheck.RegisterExpected("Context1");
        MigrationHealthCheck.RegisterExpected("Context2");
        MigrationHealthCheck.MarkComplete("Context1");

        await Assert.That(MigrationHealthCheck.IsReady).IsFalse();
    }

    [Test]
    public async Task IsReady_MultipleContexts_AllComplete_ReturnsTrue()
    {
        MigrationHealthCheck.RegisterExpected("Context1");
        MigrationHealthCheck.RegisterExpected("Context2");
        MigrationHealthCheck.MarkComplete("Context1");
        MigrationHealthCheck.MarkComplete("Context2");

        await Assert.That(MigrationHealthCheck.IsReady).IsTrue();
    }

    [Test]
    public async Task CheckHealthAsync_Pending_ReturnsUnhealthy()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        var check = new MigrationHealthCheck();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task CheckHealthAsync_AllComplete_ReturnsHealthy()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        MigrationHealthCheck.MarkComplete("TestContext");
        var check = new MigrationHealthCheck();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
    }

    [Test]
    public async Task Reset_ClearsAllState()
    {
        MigrationHealthCheck.RegisterExpected("TestContext");
        MigrationHealthCheck.MarkComplete("TestContext");

        await Assert.That(MigrationHealthCheck.IsReady).IsTrue();

        MigrationHealthCheck.Reset();

        await Assert.That(MigrationHealthCheck.IsReady).IsFalse();
    }
}
