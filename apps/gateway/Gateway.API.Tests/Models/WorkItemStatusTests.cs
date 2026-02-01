namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class WorkItemStatusTests
{
    [Test]
    public async Task WorkItemStatus_HasExpectedValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<WorkItemStatus>();

        // Assert
        await Assert.That(values).Contains(WorkItemStatus.ReadyForReview);
        await Assert.That(values).Contains(WorkItemStatus.MissingData);
        await Assert.That(values).Contains(WorkItemStatus.PayerRequirementsNotMet);
        await Assert.That(values).Contains(WorkItemStatus.Submitted);
        await Assert.That(values).Contains(WorkItemStatus.NoPaRequired);
        await Assert.That(values.Length).IsEqualTo(5);
    }

    [Test]
    public async Task WorkItemStatus_DefaultValue_IsReadyForReview()
    {
        // Arrange & Act
        var defaultStatus = default(WorkItemStatus);

        // Assert - first enum value (0) should be ReadyForReview
        await Assert.That(defaultStatus).IsEqualTo(WorkItemStatus.ReadyForReview);
    }
}
