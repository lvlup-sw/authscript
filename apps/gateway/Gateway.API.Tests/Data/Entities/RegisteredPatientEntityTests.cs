// =============================================================================
// <copyright file="RegisteredPatientEntityTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data.Entities;

using Gateway.API.Data.Entities;
using Gateway.API.Data.Mappings;
using Gateway.API.Models;

/// <summary>
/// Tests for the <see cref="RegisteredPatientEntity"/> class and its mapping extensions.
/// </summary>
public class RegisteredPatientEntityTests
{
    /// <summary>
    /// Verifies that ToModel correctly maps all properties from entity to model.
    /// </summary>
    [Test]
    public async Task ToModel_ValidEntity_MapsAllProperties()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var entity = new RegisteredPatientEntity
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = now,
            LastPolledAt = now.AddMinutes(5),
            CurrentEncounterStatus = "arrived"
        };

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.PatientId).IsEqualTo(entity.PatientId);
        await Assert.That(model.EncounterId).IsEqualTo(entity.EncounterId);
        await Assert.That(model.PracticeId).IsEqualTo(entity.PracticeId);
        await Assert.That(model.WorkItemId).IsEqualTo(entity.WorkItemId);
        await Assert.That(model.RegisteredAt).IsEqualTo(entity.RegisteredAt);
        await Assert.That(model.LastPolledAt).IsEqualTo(entity.LastPolledAt);
        await Assert.That(model.CurrentEncounterStatus).IsEqualTo(entity.CurrentEncounterStatus);
    }

    /// <summary>
    /// Verifies that ToEntity correctly maps all properties from model to entity.
    /// </summary>
    [Test]
    public async Task ToEntity_ValidModel_MapsAllProperties()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var model = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = now,
            LastPolledAt = now.AddMinutes(5),
            CurrentEncounterStatus = "arrived"
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        await Assert.That(entity.PatientId).IsEqualTo(model.PatientId);
        await Assert.That(entity.EncounterId).IsEqualTo(model.EncounterId);
        await Assert.That(entity.PracticeId).IsEqualTo(model.PracticeId);
        await Assert.That(entity.WorkItemId).IsEqualTo(model.WorkItemId);
        await Assert.That(entity.RegisteredAt).IsEqualTo(model.RegisteredAt);
        await Assert.That(entity.LastPolledAt).IsEqualTo(model.LastPolledAt);
        await Assert.That(entity.CurrentEncounterStatus).IsEqualTo(model.CurrentEncounterStatus);
    }

    /// <summary>
    /// Verifies that ToModel correctly handles null optional properties.
    /// </summary>
    [Test]
    public async Task ToModel_NullOptionalProperties_MapsCorrectly()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var entity = new RegisteredPatientEntity
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = now,
            LastPolledAt = null,
            CurrentEncounterStatus = null
        };

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.LastPolledAt).IsNull();
        await Assert.That(model.CurrentEncounterStatus).IsNull();
    }

    /// <summary>
    /// Verifies that ToEntity correctly handles null optional properties.
    /// </summary>
    [Test]
    public async Task ToEntity_NullOptionalProperties_MapsCorrectly()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var model = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = now
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        await Assert.That(entity.LastPolledAt).IsNull();
        await Assert.That(entity.CurrentEncounterStatus).IsNull();
    }
}
