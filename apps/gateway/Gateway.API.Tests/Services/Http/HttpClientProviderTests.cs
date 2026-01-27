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

    [Test]
    public async Task GetAuthenticatedClientAsync_WithTokenEndpoint_AcquiresToken()
    {
        // Arrange
        var options = Options.Create(new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.test/",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenEndpoint = "https://auth.test/token"
        });

        var tokenResponse = """{"access_token":"test-token","expires_in":3600}""";
        var tokenHandler = new MockHttpMessageHandler(tokenResponse, HttpStatusCode.OK);
        var tokenClient = new HttpClient(tokenHandler);

        var fhirClient = new HttpClient { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientFactory.CreateClient("EpicFhir").Returns(fhirClient);
        _httpClientFactory.CreateClient().Returns(tokenClient);

        var provider = new HttpClientProvider(_httpClientFactory, options, _logger);

        // Act
        var client = await provider.GetAuthenticatedClientAsync("EpicFhir");

        // Assert
        await Assert.That(client).IsNotNull();
        await Assert.That(client!.DefaultRequestHeaders.Authorization).IsNotNull();
        await Assert.That(client.DefaultRequestHeaders.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(client.DefaultRequestHeaders.Authorization.Parameter).IsEqualTo("test-token");
    }

    [Test]
    public async Task GetAuthenticatedClientAsync_CachesToken_UntilExpiry()
    {
        // Arrange
        var options = Options.Create(new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.test/",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenEndpoint = "https://auth.test/token"
        });

        var callCount = 0;
        var tokenHandler = new MockHttpMessageHandler(() =>
        {
            callCount++;
            return ($$$"""{"access_token":"token-{{{callCount}}}","expires_in":3600}""", HttpStatusCode.OK);
        });
        var tokenClient = new HttpClient(tokenHandler);

        var fhirClient = new HttpClient { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientFactory.CreateClient("EpicFhir").Returns(fhirClient);
        _httpClientFactory.CreateClient().Returns(tokenClient);

        var provider = new HttpClientProvider(_httpClientFactory, options, _logger);

        // Act - call twice
        var client1 = await provider.GetAuthenticatedClientAsync("EpicFhir");
        var client2 = await provider.GetAuthenticatedClientAsync("EpicFhir");

        // Assert - token endpoint called only once (cached)
        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(client1!.DefaultRequestHeaders.Authorization!.Parameter).IsEqualTo("token-1");
    }

    [Test]
    public async Task GetAuthenticatedClientAsync_TokenAcquisitionFails_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.test/",
            ClientId = "test-client",
            ClientSecret = "wrong-secret",
            TokenEndpoint = "https://auth.test/token"
        });

        var tokenHandler = new MockHttpMessageHandler("Unauthorized", HttpStatusCode.Unauthorized);
        var tokenClient = new HttpClient(tokenHandler);

        _httpClientFactory.CreateClient().Returns(tokenClient);

        var provider = new HttpClientProvider(_httpClientFactory, options, _logger);

        // Act
        var client = await provider.GetAuthenticatedClientAsync("EpicFhir");

        // Assert
        await Assert.That(client).IsNull();
    }

    // Helper classes
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<(string, HttpStatusCode)> _responseFactory;

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
            : this(() => (response, statusCode)) { }

        public MockHttpMessageHandler(Func<(string, HttpStatusCode)> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (response, statusCode) = _responseFactory();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response)
            });
        }
    }
}
