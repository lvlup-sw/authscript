// =============================================================================
// <copyright file="MigrationHealthCheck.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.API.Data;

/// <summary>
/// Health check to monitor the completion status of database migrations.
/// </summary>
public class MigrationHealthCheck : IHealthCheck
{
    private static readonly ConcurrentDictionary<string, bool> s_completedMigrations = new();

    /// <summary>
    /// Marks a migration as complete for the specified context.
    /// </summary>
    /// <param name="contextName">The name of the DbContext that completed migration.</param>
    public static void MarkComplete(string contextName) =>
        s_completedMigrations[contextName] = true;

    /// <summary>
    /// Registers an expected migration for the specified context.
    /// </summary>
    /// <param name="contextName">The name of the DbContext that is expected to complete migration.</param>
    public static void RegisterExpected(string contextName) =>
        s_completedMigrations[contextName] = false;

    /// <summary>
    /// Checks the health of database migrations.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A health check result indicating the status of migrations.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var incomplete = s_completedMigrations
            .Where(kvp => !kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        return Task.FromResult(incomplete.Count != 0
            ? HealthCheckResult.Unhealthy($"Migrations pending: {string.Join(", ", incomplete)}")
            : HealthCheckResult.Healthy("All migrations completed"));
    }
}
