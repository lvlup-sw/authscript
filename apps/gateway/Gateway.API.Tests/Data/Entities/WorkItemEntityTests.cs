// =============================================================================
// <copyright file="WorkItemEntityTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data.Entities;

using Gateway.API.Data.Entities;
using Gateway.API.Data.Mappings;
using Gateway.API.Models;

/// <summary>
/// Tests for <see cref="WorkItemEntity"/> and its mapping extensions.
/// </summary>
public class WorkItemEntityTests
{
    [Test]
    public async Task ToModel_ValidEntity_MapsAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(5);

        var entity = new WorkItemEntity
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            ServiceRequestId = "sr-101",
            ProcedureCode = "12345",
            Status = WorkItemStatus.Pending,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.Id).IsEqualTo(entity.Id);
        await Assert.That(model.PatientId).IsEqualTo(entity.PatientId);
        await Assert.That(model.EncounterId).IsEqualTo(entity.EncounterId);
        await Assert.That(model.ServiceRequestId).IsEqualTo(entity.ServiceRequestId);
        await Assert.That(model.ProcedureCode).IsEqualTo(entity.ProcedureCode);
        await Assert.That(model.Status).IsEqualTo(entity.Status);
        await Assert.That(model.CreatedAt).IsEqualTo(entity.CreatedAt);
        await Assert.That(model.UpdatedAt).IsEqualTo(entity.UpdatedAt);
    }

    [Test]
    public async Task ToModel_NullOptionalProperties_MapsCorrectly()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        var entity = new WorkItemEntity
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            ServiceRequestId = null,
            ProcedureCode = null,
            Status = WorkItemStatus.Pending,
            CreatedAt = createdAt,
            UpdatedAt = null
        };

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.ServiceRequestId).IsNull();
        await Assert.That(model.ProcedureCode).IsNull();
        await Assert.That(model.UpdatedAt).IsNull();
    }

    [Test]
    public async Task ToEntity_ValidModel_MapsAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(5);

        var model = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            ServiceRequestId = "sr-101",
            ProcedureCode = "12345",
            Status = WorkItemStatus.ReadyForReview,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        await Assert.That(entity.Id).IsEqualTo(model.Id);
        await Assert.That(entity.PatientId).IsEqualTo(model.PatientId);
        await Assert.That(entity.EncounterId).IsEqualTo(model.EncounterId);
        await Assert.That(entity.ServiceRequestId).IsEqualTo(model.ServiceRequestId);
        await Assert.That(entity.ProcedureCode).IsEqualTo(model.ProcedureCode);
        await Assert.That(entity.Status).IsEqualTo(model.Status);
        await Assert.That(entity.CreatedAt).IsEqualTo(model.CreatedAt);
        await Assert.That(entity.UpdatedAt).IsEqualTo(model.UpdatedAt);
    }

    [Test]
    public async Task ToEntity_NullOptionalProperties_MapsCorrectly()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        var model = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            ServiceRequestId = null,
            ProcedureCode = null,
            Status = WorkItemStatus.Pending,
            CreatedAt = createdAt,
            UpdatedAt = null
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        await Assert.That(entity.ServiceRequestId).IsNull();
        await Assert.That(entity.ProcedureCode).IsNull();
        await Assert.That(entity.UpdatedAt).IsNull();
    }

    [Test]
    public async Task RoundTrip_ModelToEntityToModel_PreservesAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(10);

        var originalModel = new WorkItem
        {
            Id = "wi-roundtrip",
            PatientId = "patient-roundtrip",
            EncounterId = "enc-roundtrip",
            ServiceRequestId = "sr-roundtrip",
            ProcedureCode = "99999",
            Status = WorkItemStatus.Submitted,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var entity = originalModel.ToEntity();
        var resultModel = entity.ToModel();

        // Assert
        await Assert.That(resultModel.Id).IsEqualTo(originalModel.Id);
        await Assert.That(resultModel.PatientId).IsEqualTo(originalModel.PatientId);
        await Assert.That(resultModel.EncounterId).IsEqualTo(originalModel.EncounterId);
        await Assert.That(resultModel.ServiceRequestId).IsEqualTo(originalModel.ServiceRequestId);
        await Assert.That(resultModel.ProcedureCode).IsEqualTo(originalModel.ProcedureCode);
        await Assert.That(resultModel.Status).IsEqualTo(originalModel.Status);
        await Assert.That(resultModel.CreatedAt).IsEqualTo(originalModel.CreatedAt);
        await Assert.That(resultModel.UpdatedAt).IsEqualTo(originalModel.UpdatedAt);
    }

    [Test]
    [Arguments(WorkItemStatus.Pending)]
    [Arguments(WorkItemStatus.ReadyForReview)]
    [Arguments(WorkItemStatus.MissingData)]
    [Arguments(WorkItemStatus.PayerRequirementsNotMet)]
    [Arguments(WorkItemStatus.Submitted)]
    [Arguments(WorkItemStatus.NoPaRequired)]
    public async Task ToModel_AllStatusValues_MapsCorrectly(WorkItemStatus status)
    {
        // Arrange
        var entity = new WorkItemEntity
        {
            Id = "wi-status-test",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.Status).IsEqualTo(status);
    }
}
