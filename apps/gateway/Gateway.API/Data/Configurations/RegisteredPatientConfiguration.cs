// =============================================================================
// <copyright file="RegisteredPatientConfiguration.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Configurations;

using Gateway.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework Core configuration for the <see cref="RegisteredPatientEntity"/> entity.
/// Configures table mapping, column constraints, and indexes.
/// </summary>
public sealed class RegisteredPatientConfiguration : IEntityTypeConfiguration<RegisteredPatientEntity>
{
    /// <summary>
    /// Configures the entity mapping for <see cref="RegisteredPatientEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<RegisteredPatientEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("registered_patients");

        builder.HasKey(e => e.PatientId);

        builder.Property(e => e.PatientId)
            .HasMaxLength(100);

        builder.Property(e => e.EncounterId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PracticeId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.WorkItemId)
            .HasMaxLength(36) // "wi-" (3) + Guid N-format (32) + buffer = 36
            .IsRequired();

        builder.Property(e => e.RegisteredAt)
            .IsRequired();

        builder.Property(e => e.CurrentEncounterStatus)
            .HasMaxLength(50);

        builder.HasIndex(e => e.RegisteredAt)
            .HasDatabaseName("idx_registered_patients_registered");
    }
}
