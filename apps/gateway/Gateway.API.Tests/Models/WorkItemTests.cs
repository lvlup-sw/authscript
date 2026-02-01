namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class WorkItemTests
{
    [Test]
    public async Task WorkItem_RequiredProperties_InitializesCorrectly()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var workItem = new WorkItem
        {
            Id = "wi-123",
            EncounterId = "enc-456",
            PatientId = "pat-789",
            ServiceRequestId = "sr-101",
            Status = WorkItemStatus.ReadyForReview,
            ProcedureCode = "72148",
            CreatedAt = createdAt
        };

        // Assert
        await Assert.That(workItem.Id).IsEqualTo("wi-123");
        await Assert.That(workItem.EncounterId).IsEqualTo("enc-456");
        await Assert.That(workItem.PatientId).IsEqualTo("pat-789");
        await Assert.That(workItem.ServiceRequestId).IsEqualTo("sr-101");
        await Assert.That(workItem.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
        await Assert.That(workItem.ProcedureCode).IsEqualTo("72148");
        await Assert.That(workItem.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(workItem.UpdatedAt).IsNull();
    }

    [Test]
    public async Task WorkItem_OptionalProperties_CanBeSet()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var workItem = new WorkItem
        {
            Id = "wi-123",
            EncounterId = "enc-456",
            PatientId = "pat-789",
            ServiceRequestId = "sr-101",
            Status = WorkItemStatus.MissingData,
            ProcedureCode = "72148",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        await Assert.That(workItem.UpdatedAt).IsEqualTo(updatedAt);
        await Assert.That(workItem.Status).IsEqualTo(WorkItemStatus.MissingData);
    }
}
