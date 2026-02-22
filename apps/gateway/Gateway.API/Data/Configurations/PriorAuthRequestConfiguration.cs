// =============================================================================
// <copyright file="PriorAuthRequestConfiguration.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Configurations;

using Gateway.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework Core configuration for <see cref="PriorAuthRequestEntity"/>.
/// </summary>
public sealed class PriorAuthRequestConfiguration : IEntityTypeConfiguration<PriorAuthRequestEntity>
{
    /// <summary>
    /// Configures the entity type for the prior_auth_requests table.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<PriorAuthRequestEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("prior_auth_requests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(20);

        builder.Property(e => e.PatientId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.FhirPatientId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PatientName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.PatientMrn)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.PatientDob)
            .HasMaxLength(20);

        builder.Property(e => e.PatientMemberId)
            .HasMaxLength(50);

        builder.Property(e => e.PatientPayer)
            .HasMaxLength(200);

        builder.Property(e => e.PatientAddress)
            .HasMaxLength(500);

        builder.Property(e => e.PatientPhone)
            .HasMaxLength(30);

        builder.Property(e => e.ProcedureCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ProcedureName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.DiagnosisCode)
            .HasMaxLength(20);

        builder.Property(e => e.DiagnosisName)
            .HasMaxLength(200);

        builder.Property(e => e.ProviderId)
            .HasMaxLength(50);

        builder.Property(e => e.ProviderName)
            .HasMaxLength(200);

        builder.Property(e => e.ProviderNpi)
            .HasMaxLength(20);

        builder.Property(e => e.ServiceDate)
            .HasMaxLength(20);

        builder.Property(e => e.PlaceOfService)
            .HasMaxLength(100);

        builder.Property(e => e.ClinicalSummary);

        builder.Property(e => e.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Confidence)
            .IsRequired();

        builder.Property(e => e.CriteriaJson)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_prior_auth_requests_status");

        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_prior_auth_requests_created_at");

        builder.HasIndex(e => e.PatientId)
            .HasDatabaseName("idx_prior_auth_requests_patient");
    }
}
