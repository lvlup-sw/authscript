// =============================================================================
// <copyright file="GatewayDbContextFactory.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for creating GatewayDbContext instances.
/// Used by EF Core tools for migrations.
/// </summary>
public sealed class GatewayDbContextFactory : IDesignTimeDbContextFactory<GatewayDbContext>
{
    /// <inheritdoc/>
    public GatewayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GatewayDbContext>();

        // Use a dummy connection string for design-time operations
        // The actual connection comes from Aspire at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=gateway_design;Username=postgres;Password=postgres");

        return new GatewayDbContext(optionsBuilder.Options);
    }
}
