// =============================================================================
// <copyright file="PostgresWorkItemStoreTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Services;

using Gateway.API.Data;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Tests for PostgresWorkItemStore CRUD operations using EF Core InMemory database.
/// </summary>
public class PostgresWorkItemStoreTests
{
    private static GatewayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GatewayDbContext(options);
    }

    private static PostgresWorkItemStore CreateStore(GatewayDbContext context)
    {
        return new PostgresWorkItemStore(context);
    }

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ValidWorkItem_PersistsToDatabase()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var workItem = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            ServiceRequestId = "sr-001",
            ProcedureCode = "72148",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        await store.CreateAsync(workItem);

        // Assert
        var saved = await context.WorkItems.FindAsync("wi-123");
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.PatientId).IsEqualTo("patient-456");
        await Assert.That(saved.EncounterId).IsEqualTo("enc-789");
        await Assert.That(saved.ServiceRequestId).IsEqualTo("sr-001");
        await Assert.That(saved.ProcedureCode).IsEqualTo("72148");
        await Assert.That(saved.Status).IsEqualTo(WorkItemStatus.Pending);
    }

    [Test]
    public async Task CreateAsync_ReturnsWorkItemId()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var workItem = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await store.CreateAsync(workItem);

        // Assert
        await Assert.That(result).IsEqualTo("wi-123");
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsWorkItem()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var workItem = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            ServiceRequestId = "sr-001",
            ProcedureCode = "72148",
            Status = WorkItemStatus.ReadyForReview,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await store.CreateAsync(workItem);

        // Act
        var result = await store.GetByIdAsync("wi-123");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo("wi-123");
        await Assert.That(result.PatientId).IsEqualTo("patient-456");
        await Assert.That(result.EncounterId).IsEqualTo("enc-789");
        await Assert.That(result.ServiceRequestId).IsEqualTo("sr-001");
        await Assert.That(result.ProcedureCode).IsEqualTo("72148");
        await Assert.That(result.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var result = await store.GetByIdAsync("non-existent");

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Test]
    public async Task UpdateStatusAsync_ExistingId_UpdatesStatus()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var workItem = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await store.CreateAsync(workItem);

        // Act
        var result = await store.UpdateStatusAsync("wi-123", WorkItemStatus.ReadyForReview);

        // Assert
        await Assert.That(result).IsTrue();
        var updated = await store.GetByIdAsync("wi-123");
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    [Test]
    public async Task UpdateStatusAsync_ExistingId_SetsUpdatedAt()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var workItem = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await store.CreateAsync(workItem);
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        var result = await store.UpdateStatusAsync("wi-123", WorkItemStatus.Submitted);

        // Assert
        await Assert.That(result).IsTrue();
        var updated = await store.GetByIdAsync("wi-123");
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.UpdatedAt).IsNotNull();
        await Assert.That(updated.UpdatedAt!.Value >= beforeUpdate).IsTrue();
    }

    [Test]
    public async Task UpdateStatusAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var result = await store.UpdateStatusAsync("non-existent", WorkItemStatus.Submitted);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ExistingId_UpdatesAllFields()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var original = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            ServiceRequestId = null,
            ProcedureCode = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await store.CreateAsync(original);

        var updated = original with
        {
            ServiceRequestId = "sr-999",
            ProcedureCode = "72148",
            Status = WorkItemStatus.ReadyForReview,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await store.UpdateAsync("wi-123", updated);

        // Assert
        await Assert.That(result).IsTrue();
        var retrieved = await store.GetByIdAsync("wi-123");
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.ServiceRequestId).IsEqualTo("sr-999");
        await Assert.That(retrieved.ProcedureCode).IsEqualTo("72148");
        await Assert.That(retrieved.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    [Test]
    public async Task UpdateAsync_ExistingId_SetsUpdatedAt()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var original = new WorkItem
        {
            Id = "wi-123",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await store.CreateAsync(original);

        var updateTime = DateTimeOffset.UtcNow;
        var updated = original with
        {
            Status = WorkItemStatus.MissingData,
            UpdatedAt = updateTime,
        };

        // Act
        var result = await store.UpdateAsync("wi-123", updated);

        // Assert
        await Assert.That(result).IsTrue();
        var retrieved = await store.GetByIdAsync("wi-123");
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var workItem = new WorkItem
        {
            Id = "non-existent",
            PatientId = "patient-456",
            EncounterId = "enc-789",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = await store.UpdateAsync("non-existent", workItem);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    #region GetByEncounterAsync Tests

    [Test]
    public async Task GetByEncounterAsync_MatchingEncounter_ReturnsWorkItems()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var workItem1 = new WorkItem
        {
            Id = "wi-001",
            PatientId = "patient-001",
            EncounterId = "enc-shared",
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var workItem2 = new WorkItem
        {
            Id = "wi-002",
            PatientId = "patient-002",
            EncounterId = "enc-shared",
            Status = WorkItemStatus.ReadyForReview,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var workItem3 = new WorkItem
        {
            Id = "wi-003",
            PatientId = "patient-003",
            EncounterId = "enc-different",
            Status = WorkItemStatus.Submitted,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await store.CreateAsync(workItem1);
        await store.CreateAsync(workItem2);
        await store.CreateAsync(workItem3);

        // Act
        var results = await store.GetByEncounterAsync("enc-shared");

        // Assert
        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results.Any(w => w.Id == "wi-001")).IsTrue();
        await Assert.That(results.Any(w => w.Id == "wi-002")).IsTrue();
        await Assert.That(results.Any(w => w.Id == "wi-003")).IsFalse();
    }

    [Test]
    public async Task GetByEncounterAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var results = await store.GetByEncounterAsync("non-existent-encounter");

        // Assert
        await Assert.That(results).IsNotNull();
        await Assert.That(results.Count).IsEqualTo(0);
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public async Task GetAllAsync_NoFilters_ReturnsAllItems()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var workItem1 = CreateWorkItem("wi-001", "enc-001", WorkItemStatus.Pending);
        var workItem2 = CreateWorkItem("wi-002", "enc-002", WorkItemStatus.ReadyForReview);
        var workItem3 = CreateWorkItem("wi-003", "enc-003", WorkItemStatus.Submitted);

        await store.CreateAsync(workItem1);
        await store.CreateAsync(workItem2);
        await store.CreateAsync(workItem3);

        // Act
        var results = await store.GetAllAsync();

        // Assert
        await Assert.That(results.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetAllAsync_FilterByEncounter_ReturnsMatching()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var workItem1 = CreateWorkItem("wi-001", "enc-target", WorkItemStatus.Pending);
        var workItem2 = CreateWorkItem("wi-002", "enc-other", WorkItemStatus.Pending);

        await store.CreateAsync(workItem1);
        await store.CreateAsync(workItem2);

        // Act
        var results = await store.GetAllAsync(encounterId: "enc-target");

        // Assert
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Id).IsEqualTo("wi-001");
    }

    [Test]
    public async Task GetAllAsync_FilterByStatus_ReturnsMatching()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var workItem1 = CreateWorkItem("wi-001", "enc-001", WorkItemStatus.Pending);
        var workItem2 = CreateWorkItem("wi-002", "enc-002", WorkItemStatus.ReadyForReview);

        await store.CreateAsync(workItem1);
        await store.CreateAsync(workItem2);

        // Act
        var results = await store.GetAllAsync(status: WorkItemStatus.ReadyForReview);

        // Assert
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Id).IsEqualTo("wi-002");
    }

    [Test]
    public async Task GetAllAsync_FilterByBoth_ReturnsMatching()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var workItem1 = CreateWorkItem("wi-001", "enc-target", WorkItemStatus.ReadyForReview);
        var workItem2 = CreateWorkItem("wi-002", "enc-target", WorkItemStatus.Pending);
        var workItem3 = CreateWorkItem("wi-003", "enc-other", WorkItemStatus.ReadyForReview);

        await store.CreateAsync(workItem1);
        await store.CreateAsync(workItem2);
        await store.CreateAsync(workItem3);

        // Act
        var results = await store.GetAllAsync(encounterId: "enc-target", status: WorkItemStatus.ReadyForReview);

        // Assert
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Id).IsEqualTo("wi-001");
    }

    [Test]
    public async Task GetAllAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var workItem = CreateWorkItem("wi-001", "enc-001", WorkItemStatus.Pending);
        await store.CreateAsync(workItem);

        // Act
        var results = await store.GetAllAsync(encounterId: "non-existent");

        // Assert
        await Assert.That(results).IsEmpty();
    }

    #endregion

    private static WorkItem CreateWorkItem(string id, string encounterId, WorkItemStatus status)
    {
        return new WorkItem
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = "patient-test",
            ServiceRequestId = "sr-test",
            Status = status,
            ProcedureCode = "72148",
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
