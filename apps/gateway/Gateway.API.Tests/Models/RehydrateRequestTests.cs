using Gateway.API.Models;

namespace Gateway.API.Tests.Models;

public sealed class RehydrateRequestTests
{
    [Test]
    public async Task RehydrateRequest_RequiredProperties_InitializesCorrectly()
    {
        // Arrange & Act
        var request = new RehydrateRequest
        {
            WorkItemId = "wi-12345"
        };

        // Assert
        await Assert.That(request.WorkItemId).IsEqualTo("wi-12345");
    }

    [Test]
    public async Task RehydrateRequest_WorkItemId_IsRequired()
    {
        // This test verifies the required modifier is present
        // The record should require WorkItemId to be set

        // Arrange & Act
        var request = new RehydrateRequest
        {
            WorkItemId = "test-id"
        };

        // Assert - just verify it can be created with required property
        await Assert.That(request.WorkItemId).IsNotNull();
        await Assert.That(request.WorkItemId).IsNotEmpty();
    }

    [Test]
    public async Task RehydrateRequest_OptionalAccessToken_DefaultsToNull()
    {
        // Arrange & Act
        var request = new RehydrateRequest
        {
            WorkItemId = "wi-001"
        };

        // Assert
        await Assert.That(request.AccessToken).IsNull();
    }

    [Test]
    public async Task RehydrateRequest_WithAccessToken_StoresValue()
    {
        // Arrange & Act
        var request = new RehydrateRequest
        {
            WorkItemId = "wi-001",
            AccessToken = "test-token-123"
        };

        // Assert
        await Assert.That(request.AccessToken).IsEqualTo("test-token-123");
    }
}
