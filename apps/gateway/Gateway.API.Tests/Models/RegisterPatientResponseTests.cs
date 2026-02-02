// =============================================================================
// <copyright file="RegisterPatientResponseTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class RegisterPatientResponseTests
{
    [Test]
    public async Task RegisterPatientResponse_ContainsWorkItemId()
    {
        // Arrange & Act
        var response = new RegisterPatientResponse
        {
            WorkItemId = "workitem-123",
            Message = "Patient registered successfully"
        };

        // Assert
        await Assert.That(response.WorkItemId).IsEqualTo("workitem-123");
        await Assert.That(response.Message).IsEqualTo("Patient registered successfully");
    }
}
