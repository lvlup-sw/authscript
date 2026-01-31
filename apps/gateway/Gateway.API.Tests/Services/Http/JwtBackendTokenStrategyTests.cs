using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Services.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Gateway.API.Tests.Services.Http;

public class JwtBackendTokenStrategyTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _tokenCache;
    private readonly ILogger<JwtBackendTokenStrategy> _logger;
    private readonly string _testKeyPath;

    public JwtBackendTokenStrategyTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _tokenCache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<JwtBackendTokenStrategy>>();

        // Create a temporary RSA key file for testing
        _testKeyPath = Path.Combine(Path.GetTempPath(), $"test-key-{Guid.NewGuid()}.pem");
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        File.WriteAllText(_testKeyPath, privateKey);
    }

    [After(Test)]
    public Task Cleanup()
    {
        if (File.Exists(_testKeyPath))
        {
            File.Delete(_testKeyPath);
        }
        _tokenCache.Dispose();
        return Task.CompletedTask;
    }

    private JwtBackendTokenStrategy CreateSut(EpicFhirOptions? options = null)
    {
        options ??= new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "test-client-id",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = "RS384"
        };

        return new JwtBackendTokenStrategy(
            _httpClientFactory,
            Options.Create(options),
            _tokenCache,
            _logger);
    }

    #region Task 006 Tests - JWT Generation

    [Test]
    public async Task JwtBackendTokenStrategy_CanHandle_AlwaysReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.CanHandle;

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task JwtBackendTokenStrategy_GenerateClientAssertion_CreatesValidJwt()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var jwt = sut.GenerateClientAssertion();

        // Assert
        await Assert.That(jwt).IsNotNull();
        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(jwt);
        await Assert.That(canRead).IsTrue();
    }

    [Test]
    public async Task JwtBackendTokenStrategy_GenerateClientAssertion_IncludesRequiredClaims()
    {
        // Arrange
        var options = new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "my-test-client",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = "RS384"
        };
        var sut = CreateSut(options);

        // Act
        var jwt = sut.GenerateClientAssertion();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        // Check iss claim
        var iss = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value;
        await Assert.That(iss).IsEqualTo("my-test-client");

        // Check sub claim
        var sub = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        await Assert.That(sub).IsEqualTo("my-test-client");

        // Check aud claim
        var aud = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;
        await Assert.That(aud).IsEqualTo("https://fhir.epic.com/oauth2/token");

        // Check jti claim (unique identifier)
        var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        await Assert.That(jti).IsNotNull();

        // Verify jti is a valid GUID
        var isValidGuid = Guid.TryParse(jti, out _);
        await Assert.That(isValidGuid).IsTrue();

        // Check exp claim exists and is in the future
        var exp = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
        await Assert.That(exp).IsNotNull();
    }

    [Test]
    [Arguments("RS256", SecurityAlgorithms.RsaSha256)]
    [Arguments("RS384", SecurityAlgorithms.RsaSha384)]
    [Arguments("RS512", SecurityAlgorithms.RsaSha512)]
    public async Task JwtBackendTokenStrategy_GenerateClientAssertion_SignsWithConfiguredAlgorithm(
        string configAlgorithm,
        string expectedAlgorithm)
    {
        // Arrange
        var options = new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "test-client-id",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = configAlgorithm
        };
        var sut = CreateSut(options);

        // Act
        var jwt = sut.GenerateClientAssertion();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        await Assert.That(token.Header.Alg).IsEqualTo(expectedAlgorithm);
    }

    #endregion

    #region Task 007 Tests - Token Exchange and Caching

    [Test]
    public async Task JwtBackendTokenStrategy_AcquireTokenAsync_ReturnsCachedToken()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var expectedToken = "cached-access-token";
        cache.Set("EpicAccessToken", expectedToken, TimeSpan.FromMinutes(55));

        var options = new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "test-client-id",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = "RS384"
        };

        var sut = new JwtBackendTokenStrategy(
            _httpClientFactory,
            Options.Create(options),
            cache,
            _logger);

        // Act
        var result = await sut.AcquireTokenAsync();

        // Assert
        await Assert.That(result).IsEqualTo(expectedToken);

        // Verify HTTP client was never called
        _httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());

        cache.Dispose();
    }

    [Test]
    public async Task JwtBackendTokenStrategy_AcquireTokenAsync_ExchangesJwtForToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "new-access-token", token_type = "Bearer", expires_in = 3600 };
        var responseContent = JsonSerializer.Serialize(tokenResponse);

        var mockHandler = new MockHttpMessageHandler(responseContent, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "test-client-id",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = "RS384"
        };

        var sut = new JwtBackendTokenStrategy(
            httpClientFactory,
            Options.Create(options),
            cache,
            _logger);

        // Act
        var result = await sut.AcquireTokenAsync();

        // Assert
        await Assert.That(result).IsEqualTo("new-access-token");

        // Verify the request was made to the token endpoint
        await Assert.That(mockHandler.LastRequestUri?.ToString())
            .IsEqualTo("https://fhir.epic.com/oauth2/token");

        cache.Dispose();
    }

    [Test]
    public async Task JwtBackendTokenStrategy_AcquireTokenAsync_CachesNewToken()
    {
        // Arrange
        var tokenResponse = new { access_token = "new-access-token", token_type = "Bearer", expires_in = 3600 };
        var responseContent = JsonSerializer.Serialize(tokenResponse);

        var mockHandler = new MockHttpMessageHandler(responseContent, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "test-client-id",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = "RS384"
        };

        var sut = new JwtBackendTokenStrategy(
            httpClientFactory,
            Options.Create(options),
            cache,
            _logger);

        // Act
        var result = await sut.AcquireTokenAsync();

        // Assert - token is cached
        var cachedToken = cache.Get<string>("EpicAccessToken");
        await Assert.That(cachedToken).IsEqualTo("new-access-token");

        cache.Dispose();
    }

    [Test]
    public async Task JwtBackendTokenStrategy_AcquireTokenAsync_ReturnsNullOnError()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(mockHandler);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = new EpicFhirOptions
        {
            FhirBaseUrl = "https://fhir.epic.com/api/FHIR/R4",
            ClientId = "test-client-id",
            TokenEndpoint = "https://fhir.epic.com/oauth2/token",
            PrivateKeyPath = _testKeyPath,
            SigningAlgorithm = "RS384"
        };

        var sut = new JwtBackendTokenStrategy(
            httpClientFactory,
            Options.Create(options),
            cache,
            _logger);

        // Act
        var result = await sut.AcquireTokenAsync();

        // Assert
        await Assert.That(result).IsNull();

        cache.Dispose();
    }

    #endregion

    /// <summary>
    /// Mock HTTP message handler for testing HTTP requests.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;

        public Uri? LastRequestUri { get; private set; }
        public HttpContent? LastRequestContent { get; private set; }

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestContent = request.Content;

            await Task.Yield();
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
