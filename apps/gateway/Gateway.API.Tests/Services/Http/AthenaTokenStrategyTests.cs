namespace Gateway.API.Tests.Services.Http;

using System.Net;
using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Services.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

public class AthenaTokenStrategyTests
{
    private readonly IOptions<AthenaOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;

    public AthenaTokenStrategyTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        _options = Options.Create(new AthenaOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            FhirBaseUrl = "https://api.platform.athenahealth.com/fhir/r4",
            TokenEndpoint = "https://api.platform.athenahealth.com/oauth2/token"
        });
    }

    [Test]
    public async Task AthenaTokenStrategy_CanHandle_ReturnsTrueWhenOptionsAreValid()
    {
        // Arrange
        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options);

        // Act
        var canHandle = strategy.CanHandle;

        // Assert
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task AthenaTokenStrategy_CanHandle_ReturnsFalseWhenOptionsAreInvalid()
    {
        // Arrange
        var invalidOptions = Options.Create(new AthenaOptions
        {
            ClientId = "",
            FhirBaseUrl = "https://api.platform.athenahealth.com/fhir/r4",
            TokenEndpoint = "https://api.platform.athenahealth.com/oauth2/token"
        });
        var strategy = new AthenaTokenStrategy(_httpClientFactory, invalidOptions);

        // Act
        var canHandle = strategy.CanHandle;

        // Assert
        await Assert.That(canHandle).IsFalse();
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_PostsToTokenEndpoint()
    {
        // Arrange
        var tokenResponse = new { access_token = "test-access-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options);

        // Act
        await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(_mockHandler.LastRequest).IsNotNull();
        await Assert.That(_mockHandler.LastRequest!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(_mockHandler.LastRequest.RequestUri!.ToString())
            .IsEqualTo("https://api.platform.athenahealth.com/oauth2/token");
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_SendsClientCredentialsGrant()
    {
        // Arrange
        var tokenResponse = new { access_token = "test-access-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options);

        // Act
        await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(_mockHandler.LastRequestContent).IsNotNull();
        await Assert.That(_mockHandler.LastRequestContent!).Contains("grant_type=client_credentials");
        await Assert.That(_mockHandler.LastRequestContent!).Contains("client_id=test-client-id");
        await Assert.That(_mockHandler.LastRequestContent!).Contains("client_secret=test-client-secret");
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_ReturnsAccessToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "expected-access-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options);

        // Act
        var token = await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(token).IsEqualTo("expected-access-token");
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_ReturnsNullOnError()
    {
        // Arrange
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.Unauthorized,
            "");

        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options);

        // Act
        var token = await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(token).IsNull();
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_CachesToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "cached-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var timeProvider = new FakeTimeProvider();
        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options, timeProvider);

        // Act
        await strategy.AcquireTokenAsync();
        _mockHandler.ResetRequestCount();
        await strategy.AcquireTokenAsync();

        // Assert - second call should not make HTTP request
        await Assert.That(_mockHandler.RequestCount).IsEqualTo(0);
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_ReturnsCachedToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "cached-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var timeProvider = new FakeTimeProvider();
        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options, timeProvider);

        // Act
        var firstToken = await strategy.AcquireTokenAsync();
        var secondToken = await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(firstToken).IsEqualTo("cached-token");
        await Assert.That(secondToken).IsEqualTo("cached-token");
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_RefreshesExpiredToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "first-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var timeProvider = new FakeTimeProvider();
        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options, timeProvider);

        // Act - first call
        await strategy.AcquireTokenAsync();
        _mockHandler.ResetRequestCount();

        // Advance time past expiry (3600 - 60 = 3540 seconds)
        timeProvider.Advance(TimeSpan.FromSeconds(3541));

        // Setup new response for refresh
        var refreshResponse = new { access_token = "refreshed-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(refreshResponse));

        // Act - second call after expiry
        var token = await strategy.AcquireTokenAsync();

        // Assert - should have made a new HTTP request
        await Assert.That(_mockHandler.RequestCount).IsEqualTo(1);
        await Assert.That(token).IsEqualTo("refreshed-token");
    }

    [Test]
    public async Task AthenaTokenStrategy_AcquireTokenAsync_CachesForExpiryMinus60Seconds()
    {
        // Arrange
        var tokenResponse = new { access_token = "cached-token", expires_in = 3600 };
        _mockHandler.SetupResponse(
            "https://api.platform.athenahealth.com/oauth2/token",
            HttpMethod.Post,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var timeProvider = new FakeTimeProvider();
        var strategy = new AthenaTokenStrategy(_httpClientFactory, _options, timeProvider);

        // Act - first call
        await strategy.AcquireTokenAsync();
        _mockHandler.ResetRequestCount();

        // Advance time to just before expiry (3600 - 60 - 1 = 3539 seconds)
        timeProvider.Advance(TimeSpan.FromSeconds(3539));

        // Act - should still use cached token
        await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(_mockHandler.RequestCount).IsEqualTo(0);
    }
}

/// <summary>
/// Fake time provider for testing time-dependent code.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);

    public void SetTime(DateTimeOffset time) => _utcNow = time;
}

/// <summary>
/// Helper class to mock HTTP responses in tests.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<(string Url, HttpMethod Method), (HttpStatusCode Status, string Content)> _responses = new();

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestContent { get; private set; }
    public int RequestCount { get; private set; }

    public void SetupResponse(string url, HttpMethod method, HttpStatusCode status, string content)
    {
        _responses[(url, method)] = (status, content);
    }

    public void ResetRequestCount() => RequestCount = 0;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        RequestCount++;

        if (request.Content != null)
        {
            LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var key = (request.RequestUri!.ToString(), request.Method);
        if (_responses.TryGetValue(key, out var response))
        {
            return new HttpResponseMessage(response.Status)
            {
                Content = new StringContent(response.Content)
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
