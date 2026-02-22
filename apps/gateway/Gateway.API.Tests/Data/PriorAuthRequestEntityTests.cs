// =============================================================================
// <copyright file="PriorAuthRequestEntityTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data.Entities;

/// <summary>
/// Tests for <see cref="PriorAuthRequestEntity"/>.
/// </summary>
public class PriorAuthRequestEntityTests
{
    [Test]
    public async Task Entity_CanBeConstructed_WithRequiredProperties()
    {
        // Arrange & Act
        var entity = new PriorAuthRequestEntity
        {
            Id = "pa-001",
            PatientId = "60178",
            FhirPatientId = "a-195900.E-60178",
            PatientName = "John Doe",
            PatientMrn = "MRN001",
            ProcedureCode = "27447",
            ProcedureName = "Total Knee Replacement",
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        await Assert.That(entity.Id).IsEqualTo("pa-001");
        await Assert.That(entity.PatientId).IsEqualTo("60178");
        await Assert.That(entity.FhirPatientId).IsEqualTo("a-195900.E-60178");
        await Assert.That(entity.PatientName).IsEqualTo("John Doe");
        await Assert.That(entity.PatientMrn).IsEqualTo("MRN001");
        await Assert.That(entity.ProcedureCode).IsEqualTo("27447");
        await Assert.That(entity.ProcedureName).IsEqualTo("Total Knee Replacement");
        await Assert.That(entity.Status).IsEqualTo("draft");
        await Assert.That(entity.Confidence).IsEqualTo(0);
        await Assert.That(entity.ReviewTimeSeconds).IsEqualTo(0);
    }

    [Test]
    public async Task Entity_CriteriaJson_IsNullable()
    {
        // Arrange & Act
        var entity = new PriorAuthRequestEntity
        {
            Id = "pa-002",
            PatientId = "60179",
            FhirPatientId = "a-195900.E-60179",
            PatientName = "Jane Smith",
            PatientMrn = "MRN002",
            ProcedureCode = "27447",
            ProcedureName = "Total Knee Replacement",
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        await Assert.That(entity.CriteriaJson).IsNull();

        // Act - set a value
        entity.CriteriaJson = "[{\"met\":true,\"label\":\"Test\"}]";
        await Assert.That(entity.CriteriaJson).IsEqualTo("[{\"met\":true,\"label\":\"Test\"}]");
    }

    [Test]
    public async Task Entity_NullableFields_DefaultToNull()
    {
        // Arrange & Act
        var entity = new PriorAuthRequestEntity
        {
            Id = "pa-003",
            PatientId = "60180",
            FhirPatientId = "a-195900.E-60180",
            PatientName = "Bob Wilson",
            PatientMrn = "MRN003",
            ProcedureCode = "27447",
            ProcedureName = "Total Knee Replacement",
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        await Assert.That(entity.PatientDob).IsNull();
        await Assert.That(entity.PatientMemberId).IsNull();
        await Assert.That(entity.PatientPayer).IsNull();
        await Assert.That(entity.PatientAddress).IsNull();
        await Assert.That(entity.PatientPhone).IsNull();
        await Assert.That(entity.DiagnosisCode).IsNull();
        await Assert.That(entity.DiagnosisName).IsNull();
        await Assert.That(entity.ProviderId).IsNull();
        await Assert.That(entity.ProviderName).IsNull();
        await Assert.That(entity.ProviderNpi).IsNull();
        await Assert.That(entity.ServiceDate).IsNull();
        await Assert.That(entity.PlaceOfService).IsNull();
        await Assert.That(entity.ClinicalSummary).IsNull();
        await Assert.That(entity.CriteriaJson).IsNull();
        await Assert.That(entity.ReadyAt).IsNull();
        await Assert.That(entity.SubmittedAt).IsNull();
    }
}
