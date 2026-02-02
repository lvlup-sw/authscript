// =============================================================================
// <copyright file="RegisteredPatientTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

/// <summary>
/// Tests for the <see cref="RegisteredPatient"/> model.
/// </summary>
public class RegisteredPatientTests
{
    /// <summary>
    /// Verifies that all properties can be initialized on a RegisteredPatient instance.
    /// </summary>
    [Test]
    public async Task RegisteredPatient_AllProperties_CanBeInitialized()
    {
        // Arrange & Act
        var now = DateTimeOffset.UtcNow;
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc",
            RegisteredAt = now,
            LastPolledAt = now.AddMinutes(5),
            CurrentEncounterStatus = "in-progress"
        };

        // Assert
        await Assert.That(patient.PatientId).IsEqualTo("patient-123");
        await Assert.That(patient.EncounterId).IsEqualTo("encounter-456");
        await Assert.That(patient.PracticeId).IsEqualTo("practice-789");
        await Assert.That(patient.WorkItemId).IsEqualTo("workitem-abc");
        await Assert.That(patient.RegisteredAt).IsEqualTo(now);
        await Assert.That(patient.LastPolledAt).IsEqualTo(now.AddMinutes(5));
        await Assert.That(patient.CurrentEncounterStatus).IsEqualTo("in-progress");
    }

    /// <summary>
    /// Verifies that optional properties can be null.
    /// </summary>
    [Test]
    public async Task RegisteredPatient_OptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var now = DateTimeOffset.UtcNow;
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc",
            RegisteredAt = now
        };

        // Assert
        await Assert.That(patient.LastPolledAt).IsNull();
        await Assert.That(patient.CurrentEncounterStatus).IsNull();
    }
}
