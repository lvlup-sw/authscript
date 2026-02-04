// =============================================================================
// <copyright file="GatewayDbContextTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data;
using Gateway.API.Data.Entities;
using Gateway.API.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Tests for <see cref="GatewayDbContext"/>.
/// </summary>
public class GatewayDbContextTests
{
    private static GatewayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GatewayDbContext(options);
    }

    [Test]
    public async Task GatewayDbContext_HasWorkItemsDbSet()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var dbSet = context.WorkItems;

        // Assert
        await Assert.That(dbSet).IsNotNull();
        await Assert.That(dbSet).IsAssignableTo<DbSet<WorkItemEntity>>();
    }

    [Test]
    public async Task GatewayDbContext_HasRegisteredPatientsDbSet()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var dbSet = context.RegisteredPatients;

        // Assert
        await Assert.That(dbSet).IsNotNull();
        await Assert.That(dbSet).IsAssignableTo<DbSet<RegisteredPatientEntity>>();
    }

    [Test]
    public async Task GatewayDbContext_CanAddAndRetrieveWorkItem()
    {
        // Arrange
        using var context = CreateContext();
        var workItem = new WorkItemEntity
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync();

        var retrieved = await context.WorkItems.FindAsync("wi-123");

        // Assert
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.PatientId).IsEqualTo("patient-456");
    }

    [Test]
    public async Task GatewayDbContext_CanAddAndRetrieveRegisteredPatient()
    {
        // Arrange
        using var context = CreateContext();
        var patient = new RegisteredPatientEntity
        {
            PatientId = "patient-123",
            EncounterId = "enc-456",
            PracticeId = "practice-789",
            WorkItemId = "wi-101",
            RegisteredAt = DateTimeOffset.UtcNow,
        };

        // Act
        context.RegisteredPatients.Add(patient);
        await context.SaveChangesAsync();

        var retrieved = await context.RegisteredPatients.FindAsync("patient-123");

        // Assert
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.EncounterId).IsEqualTo("enc-456");
    }

    [Test]
    public async Task GatewayDbContext_AppliesWorkItemConfiguration()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(WorkItemEntity));

        // Assert
        await Assert.That(entityType).IsNotNull();
        await Assert.That(entityType!.GetTableName()).IsEqualTo("work_items");

        var idProperty = entityType!.FindProperty(nameof(WorkItemEntity.Id));
        await Assert.That(idProperty).IsNotNull();
        await Assert.That(idProperty!.GetMaxLength()).IsEqualTo(36);
    }

    [Test]
    public async Task GatewayDbContext_AppliesRegisteredPatientConfiguration()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(RegisteredPatientEntity));

        // Assert
        await Assert.That(entityType).IsNotNull();
        await Assert.That(entityType!.GetTableName()).IsEqualTo("registered_patients");

        var patientIdProperty = entityType!.FindProperty(nameof(RegisteredPatientEntity.PatientId));
        await Assert.That(patientIdProperty).IsNotNull();
        await Assert.That(patientIdProperty!.GetMaxLength()).IsEqualTo(100);
    }
}
