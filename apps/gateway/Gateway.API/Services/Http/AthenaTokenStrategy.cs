namespace Gateway.API.Services.Http;

using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts.Http;
using Microsoft.Extensions.Options;

/// <summary>
/// Token acquisition strategy for athenahealth using OAuth 2.0 client credentials flow.
/// </summary>
public sealed class AthenaTokenStrategy : ITokenAcquisitionStrategy
{
    private const int TokenExpiryBufferSeconds = 60;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AthenaOptions _options;
    private readonly TimeProvider _timeProvider;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="AthenaTokenStrategy"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for making token requests.</param>
    /// <param name="options">athenahealth configuration options.</param>
    public AthenaTokenStrategy(IHttpClientFactory httpClientFactory, IOptions<AthenaOptions> options)
        : this(httpClientFactory, options, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AthenaTokenStrategy"/> class with a custom time provider.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for making token requests.</param>
    /// <param name="options">athenahealth configuration options.</param>
    /// <param name="timeProvider">Time provider for token expiry calculations.</param>
    public AthenaTokenStrategy(
        IHttpClientFactory httpClientFactory,
        IOptions<AthenaOptions> options,
        TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public bool CanHandle => _options.IsValid();

    /// <inheritdoc />
    public async Task<string?> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        // Return cached token if still valid
        if (IsCachedTokenValid())
        {
            return _cachedToken;
        }

        var client = _httpClientFactory.CreateClient("Athena");

        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret ?? string.Empty
        });

        try
        {
            var response = await client.PostAsync(_options.TokenEndpoint, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

            if (tokenResponse?.AccessToken is null)
            {
                return null;
            }

            // Cache token with expiry buffer
            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = _timeProvider.GetUtcNow()
                .AddSeconds(tokenResponse.ExpiresIn - TokenExpiryBufferSeconds);

            return _cachedToken;
        }
        catch
        {
            return null;
        }
    }

    private bool IsCachedTokenValid()
    {
        return _cachedToken is not null && _timeProvider.GetUtcNow() < _tokenExpiry;
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")]
        string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        int ExpiresIn);
}
