// =============================================================================
// <copyright file="MigrationService.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Gateway.API.Data;

/// <summary>
/// Background service that handles database migrations for a specific DbContext.
/// </summary>
/// <typeparam name="TContext">The DbContext type to migrate.</typeparam>
public sealed class MigrationService<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationService<TContext>> _logger;
    private readonly IOptions<MigrationServiceOptions> _options;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Activity source name for OpenTelemetry tracing.
    /// </summary>
    public const string ActivitySourceName = "Migrations";

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationService{TContext}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The migration service options.</param>
    /// <param name="environment">The hosting environment.</param>
    public MigrationService(
        IServiceProvider serviceProvider,
        ILogger<MigrationService<TContext>> logger,
        IOptions<MigrationServiceOptions> options,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _environment = environment;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var contextName = typeof(TContext).Name;
        MigrationHealthCheck.RegisterExpected(contextName);

        // Add startup delay for container databases in development
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("Waiting for database container to be ready...");
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Migration startup cancelled - host is shutting down.");
                return;
            }
        }

        try
        {
            _logger.LogInformation("Starting migration for {ContextName}...", contextName);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            if (_options.Value.MigrateOnStartup)
            {
                if (_options.Value.RecreateDatabase)
                {
                    await RecreateDatabaseAsync(context, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ValidateDatabaseExistsAsync(context, cancellationToken).ConfigureAwait(false);
                }

                await RunMigrationAsync(context, cancellationToken).ConfigureAwait(false);
            }

            MigrationHealthCheck.MarkComplete(contextName);
            _logger.LogInformation("Migration process completed successfully for {ContextName}.", contextName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.GetBaseException(), "An error occurred during the migration process for {ContextName}.", contextName);
            throw;
        }
    }

    private async Task ValidateDatabaseExistsAsync(TContext context, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            if (await context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Database exists for {ContextName}.", typeof(TContext).Name);
                return;
            }

            _logger.LogWarning("Database does not exist for {ContextName}.", typeof(TContext).Name);

            if (context.Database.GetMigrations().Any())
            {
                _logger.LogInformation("Migrations are defined for {ContextName}. MigrateAsync will create the database and apply migrations.", typeof(TContext).Name);
                return;
            }

            _logger.LogInformation("Creating the database for {ContextName}...", typeof(TContext).Name);
            await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Database created successfully for {ContextName}.", typeof(TContext).Name);
        }).ConfigureAwait(false);
    }

    private async Task RunMigrationAsync(TContext context, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            try
            {
                try
                {
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
                    IEnumerable<string> migrations = pendingMigrations.ToList();
                    _logger.LogInformation("Found {Count} pending migrations for {ContextName}: {Migrations}",
                        migrations.Count(),
                        typeof(TContext).Name,
                        string.Join(", ", migrations));
                }
                catch (PostgresException ex) when (ex.SqlState == "42P01")
                {
                    _logger.LogInformation("Migration history table does not exist for {ContextName}. All migrations will be applied.", typeof(TContext).Name);
                }

                _logger.LogInformation("Applying migrations for {ContextName}...", typeof(TContext).Name);
                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Successfully applied migrations for {ContextName}. Total applied: {Count}",
                        typeof(TContext).Name,
                        appliedMigrations.Count());
                }
                catch (Exception verifyEx)
                {
                    _logger.LogWarning(verifyEx, "Could not verify applied migrations for {ContextName}, but continuing...", typeof(TContext).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply migrations for {ContextName}", typeof(TContext).Name);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task RecreateDatabaseAsync(TContext context, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            _logger.LogWarning("RECREATING DATABASE for {ContextName}. All existing data will be lost!", typeof(TContext).Name);

            try
            {
                if (await context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
                {
                    await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("All database objects dropped successfully for {ContextName}.", typeof(TContext).Name);
                }
                else
                {
                    _logger.LogInformation("Database connection not available for {ContextName}, assuming clean state.", typeof(TContext).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not drop database for {ContextName}, proceeding with migration anyway.", typeof(TContext).Name);
            }
        }).ConfigureAwait(false);
    }
}
