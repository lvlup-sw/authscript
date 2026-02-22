// =============================================================================
// <copyright file="IDataSeeder.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Microsoft.EntityFrameworkCore;

namespace Gateway.API.Data;

/// <summary>
/// Interface for seeding initial data into a database context after migration.
/// Implementations should be idempotent (safe to run multiple times).
/// </summary>
/// <typeparam name="TContext">The DbContext type this seeder targets.</typeparam>
public interface IDataSeeder<in TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Seeds data into the database. Must be idempotent.
    /// </summary>
    /// <param name="context">The database context to seed data into.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SeedAsync(TContext context, CancellationToken cancellationToken);
}
