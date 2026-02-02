namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class WorkItemListResponseTests
{
    [Test]
    public async Task WorkItemListResponse_RequiredProperties_InitializesCorrectly()
    {
        // Arrange
        var items = new List<WorkItem>
        {
            new WorkItem
            {
                Id = "wi-001",
                EncounterId = "enc-001",
                PatientId = "pat-001",
                ServiceRequestId = "sr-001",
                Status = WorkItemStatus.MissingData,
                ProcedureCode = "72148",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act
        var response = new WorkItemListResponse
        {
            Items = items,
            Total = 1
        };

        // Assert
        await Assert.That(response.Items).HasCount().EqualTo(1);
        await Assert.That(response.Total).IsEqualTo(1);
    }

    [Test]
    public async Task WorkItemListResponse_EmptyList_ValidState()
    {
        // Arrange & Act
        var response = new WorkItemListResponse
        {
            Items = [],
            Total = 0
        };

        // Assert
        await Assert.That(response.Items).IsEmpty();
        await Assert.That(response.Total).IsEqualTo(0);
    }

    [Test]
    public async Task WorkItemListResponse_MultipleItems_CountsCorrectly()
    {
        // Arrange
        var items = new List<WorkItem>
        {
            CreateTestWorkItem("wi-001"),
            CreateTestWorkItem("wi-002"),
            CreateTestWorkItem("wi-003")
        };

        // Act
        var response = new WorkItemListResponse
        {
            Items = items,
            Total = 3
        };

        // Assert
        await Assert.That(response.Items).HasCount().EqualTo(3);
        await Assert.That(response.Total).IsEqualTo(3);
    }

    private static WorkItem CreateTestWorkItem(string id)
    {
        return new WorkItem
        {
            Id = id,
            EncounterId = "enc-001",
            PatientId = "pat-001",
            ServiceRequestId = "sr-001",
            Status = WorkItemStatus.MissingData,
            ProcedureCode = "72148",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
