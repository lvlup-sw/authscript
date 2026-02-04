// =============================================================================
// <copyright file="WorkItemConfiguration.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Configurations;

using Gateway.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework Core configuration for <see cref="WorkItemEntity"/>.
/// </summary>
public sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItemEntity>
{
    /// <summary>
    /// Configures the entity type for the work_items table.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<WorkItemEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("work_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(36); // "wi-" (3) + Guid N-format (32) + buffer = 36

        builder.Property(e => e.PatientId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EncounterId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ServiceRequestId)
            .HasMaxLength(100);

        builder.Property(e => e.ProcedureCode)
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.EncounterId)
            .HasDatabaseName("idx_work_items_encounter");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_work_items_status");
    }
}
