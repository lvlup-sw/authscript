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
}
