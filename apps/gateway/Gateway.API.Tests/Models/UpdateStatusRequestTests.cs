using Gateway.API.Models;

namespace Gateway.API.Tests.Models;

public sealed class UpdateStatusRequestTests
{
    [Test]
    public async Task UpdateStatusRequest_RequiredStatus_InitializesCorrectly()
    {
        // Arrange & Act
        var request = new UpdateStatusRequest
        {
            Status = WorkItemStatus.ReadyForReview
        };

        // Assert
        await Assert.That(request.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    [Test]
    [Arguments(WorkItemStatus.MissingData)]
    [Arguments(WorkItemStatus.ReadyForReview)]
    [Arguments(WorkItemStatus.PayerRequirementsNotMet)]
    [Arguments(WorkItemStatus.Submitted)]
    [Arguments(WorkItemStatus.NoPaRequired)]
    public async Task UpdateStatusRequest_AllStatusValues_Accepted(WorkItemStatus status)
    {
        // Arrange & Act
        var request = new UpdateStatusRequest
        {
            Status = status
        };

        // Assert
        await Assert.That(request.Status).IsEqualTo(status);
    }
}
