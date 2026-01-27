namespace Gateway.API.Services.Http;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts.Http;
using Microsoft.Extensions.Options;

/// <summary>
/// Provides authenticated HTTP clients using client credentials flow.
/// </summary>
public sealed class HttpClientProvider : IHttpClientProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EpicFhirOptions _epicOptions;
    private readonly ILogger<HttpClientProvider> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="epicOptions">Epic FHIR configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public HttpClientProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<EpicFhirOptions> epicOptions,
        ILogger<HttpClientProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _epicOptions = epicOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HttpClient?> GetAuthenticatedClientAsync(
        string clientName,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(clientName);

        if (string.IsNullOrEmpty(_epicOptions.TokenEndpoint))
        {
            _logger.LogDebug("No token endpoint configured, returning unauthenticated client");
            return client;
        }

        var token = await GetOrRefreshTokenAsync(cancellationToken);
        if (token is null)
        {
            _logger.LogError("Failed to acquire access token");
            return null;
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private async Task<string?> GetOrRefreshTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        try
        {
            using var tokenClient = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _epicOptions.ClientId,
                ["client_secret"] = _epicOptions.ClientSecret ?? ""
            });

            var response = await tokenClient.PostAsync(_epicOptions.TokenEndpoint, content, ct);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
            if (tokenResponse is null) return null;

            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire token from {Endpoint}", _epicOptions.TokenEndpoint);
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")]
        string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        int ExpiresIn);
}
