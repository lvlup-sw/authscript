// =============================================================================
// <copyright file="PostgresPatientRegistryTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Services;

using Gateway.API.Data;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Tests for the PostgresPatientRegistry service.
/// Uses EF Core InMemory database for testing persistence behavior.
/// </summary>
public class PostgresPatientRegistryTests
{
    private static GatewayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GatewayDbContext(options);
    }

    private static PostgresPatientRegistry CreateRegistry(GatewayDbContext context)
    {
        return new PostgresPatientRegistry(context);
    }

    private static RegisteredPatient CreatePatient(string patientId) => new()
    {
        PatientId = patientId,
        EncounterId = $"enc-{patientId}",
        PracticeId = "practice-1",
        WorkItemId = $"wi-{patientId}",
        RegisteredAt = DateTimeOffset.UtcNow
    };

    [Test]
    public async Task RegisterAsync_ValidPatient_PersistsToDatabase()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = DateTimeOffset.UtcNow
        };

        // Act
        await registry.RegisterAsync(patient);

        // Assert
        var saved = await context.RegisteredPatients.FindAsync("patient-123");
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.EncounterId).IsEqualTo("enc-456");
        await Assert.That(saved.PracticeId).IsEqualTo("practice-789");
        await Assert.That(saved.WorkItemId).IsEqualTo("wi-101");
    }

    [Test]
    public async Task GetAsync_ExistingPatient_ReturnsPatient()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        await registry.RegisterAsync(patient);

        // Act
        var result = await registry.GetAsync("patient-123");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.PatientId).IsEqualTo("patient-123");
        await Assert.That(result.EncounterId).IsEqualTo("enc-456");
        await Assert.That(result.PracticeId).IsEqualTo("practice-789");
        await Assert.That(result.WorkItemId).IsEqualTo("wi-101");
    }

    [Test]
    public async Task GetAsync_NonExistentPatient_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);

        // Act
        var result = await registry.GetAsync("non-existent");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetActiveAsync_WithActivePatients_ReturnsAll()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);
        var patient1 = new RegisteredPatient
        {
            PatientId = "patient-1",
            EncounterId = "enc-1",
            PracticeId = "practice-1",
            WorkItemId = "wi-1",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        var patient2 = new RegisteredPatient
        {
            PatientId = "patient-2",
            EncounterId = "enc-2",
            PracticeId = "practice-1",
            WorkItemId = "wi-2",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        await registry.RegisterAsync(patient1);
        await registry.RegisterAsync(patient2);

        // Act
        var result = await registry.GetActiveAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetActiveAsync_NoPatients_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);

        // Act
        var result = await registry.GetActiveAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task UnregisterAsync_ExistingPatient_RemovesFromDatabase()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        await registry.RegisterAsync(patient);

        // Act
        await registry.UnregisterAsync("patient-123");

        // Assert
        var removed = await context.RegisteredPatients.FindAsync("patient-123");
        await Assert.That(removed).IsNull();
    }

    [Test]
    public async Task UnregisterAsync_NonExistentPatient_DoesNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);

        // Act & Assert - should not throw
        await registry.UnregisterAsync("non-existent");
    }

    [Test]
    public async Task UpdateAsync_ExistingPatient_UpdatesFields()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        await registry.RegisterAsync(patient);
        var pollTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        await registry.UpdateAsync("patient-123", pollTime, "arrived");

        // Assert
        var updated = await context.RegisteredPatients.FindAsync("patient-123");
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.LastPolledAt).IsEqualTo(pollTime);
        await Assert.That(updated.CurrentEncounterStatus).IsEqualTo("arrived");
    }

    [Test]
    public async Task UpdateAsync_ExistingPatient_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);
        var patient = new RegisteredPatient
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = DateTimeOffset.UtcNow
        };
        await registry.RegisterAsync(patient);

        // Act
        var result = await registry.UpdateAsync("patient-123", DateTimeOffset.UtcNow, "arrived");

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UpdateAsync_NonExistentPatient_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var registry = CreateRegistry(context);

        // Act
        var result = await registry.UpdateAsync("non-existent", DateTimeOffset.UtcNow, "arrived");

        // Assert
        await Assert.That(result).IsFalse();
    }
}
