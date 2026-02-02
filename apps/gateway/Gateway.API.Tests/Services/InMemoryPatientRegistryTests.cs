// =============================================================================
// <copyright file="InMemoryPatientRegistryTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.Models;
using Gateway.API.Services;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for the InMemoryPatientRegistry service.
/// </summary>
public class InMemoryPatientRegistryTests
{
    private readonly InMemoryPatientRegistry _registry = new();

    [Test]
    public async Task RegisterAsync_ValidPatient_AddsToRegistry()
    {
        // Arrange
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc",
            RegisteredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _registry.RegisterAsync(patient);

        // Assert - verify by calling GetAsync
        var retrieved = await _registry.GetAsync("patient-123");
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.PatientId).IsEqualTo("patient-123");
    }

    [Test]
    public async Task RegisterAsync_SamePatientTwice_OverwritesPrevious()
    {
        // Arrange
        var patient1 = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "encounter-1",
            PracticeId = "practice-1",
            WorkItemId = "workitem-1",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        var patient2 = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "encounter-2",
            PracticeId = "practice-2",
            WorkItemId = "workitem-2",
            RegisteredAt = DateTimeOffset.UtcNow
        };

        // Act
        await _registry.RegisterAsync(patient1);
        await _registry.RegisterAsync(patient2);

        // Assert
        var retrieved = await _registry.GetAsync("patient-123");
        await Assert.That(retrieved!.EncounterId).IsEqualTo("encounter-2");
    }

    [Test]
    public async Task GetAsync_NonExistentPatient_ReturnsNull()
    {
        // Act
        var result = await _registry.GetAsync("non-existent-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetActiveAsync_ReturnsAllActivePatients()
    {
        // Arrange
        var patient1 = CreatePatient("p1");
        var patient2 = CreatePatient("p2");
        await _registry.RegisterAsync(patient1);
        await _registry.RegisterAsync(patient2);

        // Act
        var active = await _registry.GetActiveAsync();

        // Assert
        await Assert.That(active.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetActiveAsync_FiltersExpiredPatients()
    {
        // Arrange - create patient with old RegisteredAt
        var expiredPatient = new RegisteredPatient
        {
            PatientId = "expired",
            EncounterId = "enc",
            PracticeId = "prac",
            WorkItemId = "wi",
            RegisteredAt = DateTimeOffset.UtcNow.AddHours(-13) // 13 hours ago = expired
        };
        var activePatient = CreatePatient("active");

        await _registry.RegisterAsync(expiredPatient);
        await _registry.RegisterAsync(activePatient);

        // Act
        var active = await _registry.GetActiveAsync();

        // Assert
        await Assert.That(active.Count).IsEqualTo(1);
        await Assert.That(active[0].PatientId).IsEqualTo("active");
    }

    [Test]
    public async Task UnregisterAsync_ExistingPatient_RemovesFromRegistry()
    {
        // Arrange
        var patient = CreatePatient("patient-to-unregister");
        await _registry.RegisterAsync(patient);

        // Act
        await _registry.UnregisterAsync("patient-to-unregister");

        // Assert
        var retrieved = await _registry.GetAsync("patient-to-unregister");
        await Assert.That(retrieved).IsNull();
    }

    [Test]
    public async Task UnregisterAsync_NonExistentPatient_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await _registry.UnregisterAsync("non-existent-patient");
    }

    [Test]
    public async Task UpdateAsync_ExistingPatient_UpdatesFields()
    {
        // Arrange
        var patient = CreatePatient("patient-to-update");
        await _registry.RegisterAsync(patient);
        var newLastPolled = DateTimeOffset.UtcNow.AddMinutes(5);
        var newStatus = "checked-in";

        // Act
        var result = await _registry.UpdateAsync("patient-to-update", newLastPolled, newStatus);

        // Assert
        await Assert.That(result).IsTrue();
        var updated = await _registry.GetAsync("patient-to-update");
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.LastPolledAt).IsEqualTo(newLastPolled);
        await Assert.That(updated.CurrentEncounterStatus).IsEqualTo(newStatus);
    }

    [Test]
    public async Task UpdateAsync_NonExistentPatient_ReturnsFalse()
    {
        // Act
        var result = await _registry.UpdateAsync("non-existent", DateTimeOffset.UtcNow, "status");

        // Assert
        await Assert.That(result).IsFalse();
    }

    private static RegisteredPatient CreatePatient(string patientId) => new()
    {
        PatientId = patientId,
        EncounterId = $"enc-{patientId}",
        PracticeId = "practice-1",
        WorkItemId = $"wi-{patientId}",
        RegisteredAt = DateTimeOffset.UtcNow
    };
}
