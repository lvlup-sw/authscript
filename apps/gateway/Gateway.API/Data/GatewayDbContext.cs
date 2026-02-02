// =============================================================================
// <copyright file="GatewayDbContext.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data;

using Gateway.API.Data.Configurations;
using Gateway.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core database context for the Gateway application.
/// </summary>
public sealed class GatewayDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public GatewayDbContext(DbContextOptions<GatewayDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the work items.
    /// </summary>
    public DbSet<WorkItemEntity> WorkItems => Set<WorkItemEntity>();

    /// <summary>
    /// Gets the registered patients.
    /// </summary>
    public DbSet<RegisteredPatientEntity> RegisteredPatients => Set<RegisteredPatientEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new WorkItemConfiguration());
        modelBuilder.ApplyConfiguration(new RegisteredPatientConfiguration());
    }
}
