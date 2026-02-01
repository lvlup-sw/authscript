using Gateway.API.Models;
using Gateway.API.Services;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for InMemoryWorkItemStore CRUD operations.
/// </summary>
public class InMemoryWorkItemStoreTests
{
    private readonly InMemoryWorkItemStore _sut;

    public InMemoryWorkItemStoreTests()
    {
        _sut = new InMemoryWorkItemStore();
    }

    [Test]
    public async Task CreateAsync_StoresWorkItem_ReturnsId()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Id = "wi-001",
            EncounterId = "enc-001",
            PatientId = "pat-001",
            ServiceRequestId = "sr-001",
            Status = WorkItemStatus.ReadyForReview,
            ProcedureCode = "12345",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var id = await _sut.CreateAsync(workItem);

        // Assert
        await Assert.That(id).IsEqualTo("wi-001");
    }

    [Test]
    public async Task GetByIdAsync_ExistingItem_ReturnsWorkItem()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Id = "wi-002",
            EncounterId = "enc-002",
            PatientId = "pat-002",
            ServiceRequestId = "sr-002",
            Status = WorkItemStatus.MissingData,
            ProcedureCode = "67890",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _sut.CreateAsync(workItem);

        // Act
        var result = await _sut.GetByIdAsync("wi-002");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo("wi-002");
        await Assert.That(result.EncounterId).IsEqualTo("enc-002");
        await Assert.That(result.Status).IsEqualTo(WorkItemStatus.MissingData);
    }

    [Test]
    public async Task GetByIdAsync_NonExistingItem_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync("non-existent-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpdateStatusAsync_ValidWorkItem_UpdatesStatus()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Id = "wi-003",
            EncounterId = "enc-003",
            PatientId = "pat-003",
            ServiceRequestId = "sr-003",
            Status = WorkItemStatus.ReadyForReview,
            ProcedureCode = "11111",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _sut.CreateAsync(workItem);

        // Act
        var success = await _sut.UpdateStatusAsync("wi-003", WorkItemStatus.Submitted);

        // Assert
        await Assert.That(success).IsTrue();
        var updated = await _sut.GetByIdAsync("wi-003");
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Status).IsEqualTo(WorkItemStatus.Submitted);
        await Assert.That(updated.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateStatusAsync_NonExistingItem_ReturnsFalse()
    {
        // Act
        var result = await _sut.UpdateStatusAsync("non-existent-id", WorkItemStatus.Submitted);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetByEncounterAsync_ReturnsMatchingItems()
    {
        // Arrange
        var workItem1 = new WorkItem
        {
            Id = "wi-010",
            EncounterId = "enc-shared",
            PatientId = "pat-010",
            ServiceRequestId = "sr-010",
            Status = WorkItemStatus.ReadyForReview,
            ProcedureCode = "10101",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var workItem2 = new WorkItem
        {
            Id = "wi-011",
            EncounterId = "enc-shared",
            PatientId = "pat-011",
            ServiceRequestId = "sr-011",
            Status = WorkItemStatus.MissingData,
            ProcedureCode = "20202",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var workItem3 = new WorkItem
        {
            Id = "wi-012",
            EncounterId = "enc-different",
            PatientId = "pat-012",
            ServiceRequestId = "sr-012",
            Status = WorkItemStatus.Submitted,
            ProcedureCode = "30303",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _sut.CreateAsync(workItem1);
        await _sut.CreateAsync(workItem2);
        await _sut.CreateAsync(workItem3);

        // Act
        var results = await _sut.GetByEncounterAsync("enc-shared");

        // Assert
        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results.Any(w => w.Id == "wi-010")).IsTrue();
        await Assert.That(results.Any(w => w.Id == "wi-011")).IsTrue();
        await Assert.That(results.Any(w => w.Id == "wi-012")).IsFalse();
    }

    [Test]
    public async Task GetByEncounterAsync_NoMatches_ReturnsEmptyList()
    {
        // Act
        var results = await _sut.GetByEncounterAsync("non-existent-encounter");

        // Assert
        await Assert.That(results).IsNotNull();
        await Assert.That(results.Count).IsEqualTo(0);
    }
}
