namespace Gateway.API.Tests.Services.Http;

using System.Net;
using Gateway.API.Configuration;
using Gateway.API.Contracts.Http;
using Gateway.API.Services.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

public class HttpClientProviderTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpClientProvider> _logger;

    public HttpClientProviderTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<HttpClientProvider>>();
    }

    [Test]
    public async Task GetAuthenticatedClientAsync_NoTokenEndpoint_ReturnsUnauthenticatedClient()
    {
        // Arrange
        var options = Options.Create(new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.test/",
            ClientId = "test-client",
            TokenEndpoint = null // No token endpoint
        });

        var httpClient = new HttpClient { BaseAddress = new Uri("https://fhir.test/") };
        _httpClientFactory.CreateClient("EpicFhir").Returns(httpClient);

        var provider = new HttpClientProvider(_httpClientFactory, options, _logger);

        // Act
        var client = await provider.GetAuthenticatedClientAsync("EpicFhir");

        // Assert
        await Assert.That(client).IsNotNull();
        await Assert.That(client!.DefaultRequestHeaders.Authorization).IsNull();
    }
}
