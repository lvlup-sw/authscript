using Gateway.API.Models;

namespace Gateway.API.Tests.Models;

public sealed class RehydrateRequestTests
{
    [Test]
    public async Task RehydrateRequest_DefaultsToNullAccessToken()
    {
        // Arrange & Act
        var request = new RehydrateRequest();

        // Assert
        await Assert.That(request.AccessToken).IsNull();
    }

    [Test]
    public async Task RehydrateRequest_WithAccessToken_StoresValue()
    {
        // Arrange & Act
        var request = new RehydrateRequest
        {
            AccessToken = "test-token-123"
        };

        // Assert
        await Assert.That(request.AccessToken).IsEqualTo("test-token-123");
    }
}
