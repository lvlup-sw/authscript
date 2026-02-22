// =============================================================================
// <copyright file="MigrationServiceOptions.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data;

/// <summary>
/// Configuration options for the migration service.
/// </summary>
public class MigrationServiceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to run migrations on startup.
    /// Defaults to true.
    /// </summary>
    public bool MigrateOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to drop and recreate the database before migrations.
    /// WARNING: This will delete all existing data. Only use in development environments.
    /// </summary>
    public bool RecreateDatabase { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to run data seeders after migration completes.
    /// Defaults to true. Seeders are only invoked in Development environment.
    /// </summary>
    public bool SeedData { get; set; } = true;
}
