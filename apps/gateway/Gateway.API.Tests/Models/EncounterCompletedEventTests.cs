// =============================================================================
// <copyright file="EncounterCompletedEventTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class EncounterCompletedEventTests
{
    [Test]
    public async Task EncounterCompletedEvent_AllProperties_CanBeInitialized()
    {
        // Arrange & Act
        var evt = new EncounterCompletedEvent
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc"
        };

        // Assert
        await Assert.That(evt.PatientId).IsEqualTo("patient-123");
        await Assert.That(evt.EncounterId).IsEqualTo("encounter-456");
        await Assert.That(evt.PracticeId).IsEqualTo("practice-789");
        await Assert.That(evt.WorkItemId).IsEqualTo("workitem-abc");
    }
}
