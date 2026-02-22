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
    /// Gets a value indicating whether all registered migrations have completed.
    /// Returns false if no migrations have been registered.
    /// </summary>
    public static bool IsReady =>
        s_completedMigrations.Count > 0 && s_completedMigrations.Values.All(v => v);

    /// <summary>
    /// Checks if migration is complete for the specified context.
    /// </summary>
    /// <param name="contextName">The name of the DbContext to check.</param>
    /// <returns>True if migration is complete; false otherwise.</returns>
    public static bool IsComplete(string contextName) =>
        s_completedMigrations.TryGetValue(contextName, out var complete) && complete;

    /// <summary>
    /// Waits for migration to complete for the specified context.
    /// </summary>
    /// <param name="contextName">The name of the DbContext to wait for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="pollingInterval">The interval between checks. Defaults to 100ms.</param>
    /// <returns>A task that completes when migration is done.</returns>
    public static async Task WaitForMigrationAsync(
        string contextName,
        CancellationToken cancellationToken,
        TimeSpan? pollingInterval = null)
    {
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(100);
        while (!IsComplete(contextName) && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets all migration tracking state. For testing only.
    /// </summary>
    internal static void Reset() => s_completedMigrations.Clear();

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
