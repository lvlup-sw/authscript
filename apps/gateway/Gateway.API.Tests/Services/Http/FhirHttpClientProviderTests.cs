using System.Net.Http.Headers;
using Gateway.API.Contracts.Http;
using Gateway.API.Services.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gateway.API.Tests.Services.Http;

/// <summary>
/// Tests for FhirHttpClientProvider.
/// </summary>
public class FhirHttpClientProviderTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenStrategyResolver _tokenResolver;
    private readonly ILogger<FhirHttpClientProvider> _logger;
    private readonly ITokenAcquisitionStrategy _mockStrategy;

    public FhirHttpClientProviderTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _tokenResolver = Substitute.For<ITokenStrategyResolver>();
        _logger = Substitute.For<ILogger<FhirHttpClientProvider>>();
        _mockStrategy = Substitute.For<ITokenAcquisitionStrategy>();
    }

    [Test]
    public async Task FhirHttpClientProvider_GetAuthenticatedClientAsync_UsesHttpClientFactory()
    {
        // Arrange
        var expectedClient = new HttpClient();
        _httpClientFactory.CreateClient("EpicFhir").Returns(expectedClient);
        _tokenResolver.Resolve().Returns(_mockStrategy);
        _mockStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns("test-token");

        var sut = new FhirHttpClientProvider(_httpClientFactory, _tokenResolver, _logger);

        // Act
        var result = await sut.GetAuthenticatedClientAsync();

        // Assert
        _httpClientFactory.Received(1).CreateClient("EpicFhir");
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task FhirHttpClientProvider_GetAuthenticatedClientAsync_CallsTokenResolver()
    {
        // Arrange
        var expectedClient = new HttpClient();
        _httpClientFactory.CreateClient("EpicFhir").Returns(expectedClient);
        _tokenResolver.Resolve().Returns(_mockStrategy);
        _mockStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns("test-token");

        var sut = new FhirHttpClientProvider(_httpClientFactory, _tokenResolver, _logger);

        // Act
        await sut.GetAuthenticatedClientAsync();

        // Assert
        _tokenResolver.Received(1).Resolve();
        await _mockStrategy.Received(1).AcquireTokenAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FhirHttpClientProvider_GetAuthenticatedClientAsync_AttachesBearerToken()
    {
        // Arrange
        const string expectedToken = "test-bearer-token";
        var client = new HttpClient();
        _httpClientFactory.CreateClient("EpicFhir").Returns(client);
        _tokenResolver.Resolve().Returns(_mockStrategy);
        _mockStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns(expectedToken);

        var sut = new FhirHttpClientProvider(_httpClientFactory, _tokenResolver, _logger);

        // Act
        var result = await sut.GetAuthenticatedClientAsync();

        // Assert
        await Assert.That(result.DefaultRequestHeaders.Authorization).IsNotNull();
        await Assert.That(result.DefaultRequestHeaders.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(result.DefaultRequestHeaders.Authorization!.Parameter).IsEqualTo(expectedToken);
    }

    [Test]
    public async Task FhirHttpClientProvider_GetAuthenticatedClientAsync_LogsWarningWhenNoToken()
    {
        // Arrange
        var client = new HttpClient();
        _httpClientFactory.CreateClient("EpicFhir").Returns(client);
        _tokenResolver.Resolve().Returns(_mockStrategy);
        _mockStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        var sut = new FhirHttpClientProvider(_httpClientFactory, _tokenResolver, _logger);

        // Act
        var result = await sut.GetAuthenticatedClientAsync();

        // Assert
        await Assert.That(result.DefaultRequestHeaders.Authorization).IsNull();
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("No access token")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
