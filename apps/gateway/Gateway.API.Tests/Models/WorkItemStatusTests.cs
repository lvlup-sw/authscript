// =============================================================================
// <copyright file="WorkItemStatusTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

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
        await Assert.That(values).Contains(WorkItemStatus.Pending);
        await Assert.That(values).Contains(WorkItemStatus.ReadyForReview);
        await Assert.That(values).Contains(WorkItemStatus.MissingData);
        await Assert.That(values).Contains(WorkItemStatus.PayerRequirementsNotMet);
        await Assert.That(values).Contains(WorkItemStatus.Submitted);
        await Assert.That(values).Contains(WorkItemStatus.NoPaRequired);
        await Assert.That(values.Length).IsEqualTo(6);
    }

    [Test]
    public async Task WorkItemStatus_DefaultValue_IsPending()
    {
        // Arrange & Act
        var defaultStatus = default(WorkItemStatus);

        // Assert - first enum value (0) should be Pending
        await Assert.That(defaultStatus).IsEqualTo(WorkItemStatus.Pending);
    }

    [Test]
    public async Task WorkItemStatus_AllValues_HaveCorrectOrder()
    {
        // Assert enum values are in correct order
        await Assert.That((int)WorkItemStatus.Pending).IsEqualTo(0);
        await Assert.That((int)WorkItemStatus.ReadyForReview).IsEqualTo(1);
        await Assert.That((int)WorkItemStatus.MissingData).IsEqualTo(2);
        await Assert.That((int)WorkItemStatus.PayerRequirementsNotMet).IsEqualTo(3);
        await Assert.That((int)WorkItemStatus.Submitted).IsEqualTo(4);
        await Assert.That((int)WorkItemStatus.NoPaRequired).IsEqualTo(5);
    }
}
