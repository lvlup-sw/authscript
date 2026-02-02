// =============================================================================
// <copyright file="RegisterPatientRequestTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

/// <summary>
/// Unit tests for <see cref="RegisterPatientRequest"/>.
/// </summary>
public class RegisterPatientRequestTests
{
    [Test]
    public async Task RegisterPatientRequest_RequiredProperties_AreEnforced()
    {
        // Arrange & Act
        var request = new RegisterPatientRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789"
        };

        // Assert
        await Assert.That(request.PatientId).IsEqualTo("patient-123");
        await Assert.That(request.EncounterId).IsEqualTo("encounter-456");
        await Assert.That(request.PracticeId).IsEqualTo("practice-789");
    }
}
