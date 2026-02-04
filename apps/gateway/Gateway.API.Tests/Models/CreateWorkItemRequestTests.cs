namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class CreateWorkItemRequestTests
{
    [Test]
    public async Task CreateWorkItemRequest_RequiredProperties_InitializesCorrectly()
    {
        // Arrange & Act
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-123",
            PatientId = "pat-456",
            ServiceRequestId = "sr-789",
            ProcedureCode = "72148"
        };

        // Assert
        await Assert.That(request.EncounterId).IsEqualTo("enc-123");
        await Assert.That(request.PatientId).IsEqualTo("pat-456");
        await Assert.That(request.ServiceRequestId).IsEqualTo("sr-789");
        await Assert.That(request.ProcedureCode).IsEqualTo("72148");
    }

    [Test]
    public async Task CreateWorkItemRequest_OptionalStatus_DefaultsToNull()
    {
        // Arrange & Act
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-123",
            PatientId = "pat-456",
            ServiceRequestId = "sr-789",
            ProcedureCode = "72148"
        };

        // Assert
        await Assert.That(request.Status).IsNull();
    }

    [Test]
    public async Task CreateWorkItemRequest_WithStatus_UsesProvidedValue()
    {
        // Arrange & Act
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-123",
            PatientId = "pat-456",
            ServiceRequestId = "sr-789",
            ProcedureCode = "72148",
            Status = WorkItemStatus.ReadyForReview
        };

        // Assert
        await Assert.That(request.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }
}
