namespace Gateway.API.Tests.Contracts.Http;

using Gateway.API.Contracts.Http;
using NSubstitute;

public class FhirHttpClientProviderTests
{
    [Test]
    public async Task IFhirHttpClientProvider_GetAuthenticatedClientAsync_ReturnsHttpClient()
    {
        // Arrange
        var provider = Substitute.For<IFhirHttpClientProvider>();
        var expectedClient = new HttpClient();
        provider.GetAuthenticatedClientAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedClient));

        // Act
        var client = await provider.GetAuthenticatedClientAsync();

        // Assert
        await Assert.That(client).IsNotNull();
        await Assert.That(client).IsSameReferenceAs(expectedClient);
    }

    [Test]
    public async Task IFhirHttpClientProvider_GetAuthenticatedClientAsync_AcceptsCancellationToken()
    {
        // Arrange
        var provider = Substitute.For<IFhirHttpClientProvider>();
        var cts = new CancellationTokenSource();
        var expectedClient = new HttpClient();
        provider.GetAuthenticatedClientAsync(cts.Token)
            .Returns(Task.FromResult(expectedClient));

        // Act
        var client = await provider.GetAuthenticatedClientAsync(cts.Token);

        // Assert
        await Assert.That(client).IsSameReferenceAs(expectedClient);
        await provider.Received(1).GetAuthenticatedClientAsync(cts.Token);
    }
}
