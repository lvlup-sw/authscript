using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.API.Services.Http;

/// <summary>
/// Token strategy that acquires tokens via JWT-based client credentials flow.
/// Used for backend services without user context.
/// </summary>
public sealed class JwtBackendTokenStrategy : ITokenAcquisitionStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EpicFhirOptions _options;
    private readonly IMemoryCache _tokenCache;
    private readonly ILogger<JwtBackendTokenStrategy> _logger;
    private const string CacheKey = "EpicAccessToken";

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtBackendTokenStrategy"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="options">Epic FHIR configuration options.</param>
    /// <param name="tokenCache">Memory cache for storing tokens.</param>
    /// <param name="logger">Logger instance.</param>
    public JwtBackendTokenStrategy(
        IHttpClientFactory httpClientFactory,
        IOptions<EpicFhirOptions> options,
        IMemoryCache tokenCache,
        ILogger<JwtBackendTokenStrategy> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle => true; // Fallback strategy, always can handle

    /// <inheritdoc />
    public async Task<string?> AcquireTokenAsync(CancellationToken ct = default)
    {
        // Check cache first
        if (_tokenCache.TryGetValue(CacheKey, out string? cached))
        {
            _logger.LogDebug("Using cached Epic access token");
            return cached;
        }

        try
        {
            var jwt = GenerateClientAssertion();
            var token = await ExchangeJwtForTokenAsync(jwt, ct);

            if (token is not null)
            {
                // Cache for 55 minutes (tokens typically valid for 60)
                _tokenCache.Set(CacheKey, token, TimeSpan.FromMinutes(55));
                _logger.LogInformation("Acquired and cached new Epic access token");
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Epic access token");
            return null;
        }
    }

    /// <summary>
    /// Exchanges a JWT client assertion for an access token.
    /// </summary>
    /// <param name="jwt">The signed JWT client assertion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The access token or null if the exchange failed.</returns>
    private async Task<string?> ExchangeJwtForTokenAsync(string jwt, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
            ["client_assertion"] = jwt
        });

        var response = await client.PostAsync(_options.TokenEndpoint, content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("access_token").GetString();
    }

    /// <summary>
    /// Generates a JWT client assertion for Epic OAuth.
    /// </summary>
    /// <returns>A signed JWT string.</returns>
    internal string GenerateClientAssertion()
    {
        var now = DateTime.UtcNow;
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, _options.ClientId),
            new Claim(JwtRegisteredClaimNames.Sub, _options.ClientId),
            new Claim(JwtRegisteredClaimNames.Aud, _options.TokenEndpoint ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Load private key
        var privateKey = LoadPrivateKey();
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(privateKey),
            GetSigningAlgorithm(_options.SigningAlgorithm));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(5), // JWT valid for 5 minutes
            SigningCredentials = signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RSA LoadPrivateKey()
    {
        if (string.IsNullOrEmpty(_options.PrivateKeyPath))
        {
            throw new InvalidOperationException("PrivateKeyPath is not configured");
        }

        var rsa = RSA.Create();
        var keyPem = File.ReadAllText(_options.PrivateKeyPath);
        rsa.ImportFromPem(keyPem);
        return rsa;
    }

    private static string GetSigningAlgorithm(string algorithm) => algorithm switch
    {
        "RS256" => SecurityAlgorithms.RsaSha256,
        "RS384" => SecurityAlgorithms.RsaSha384,
        "RS512" => SecurityAlgorithms.RsaSha512,
        _ => SecurityAlgorithms.RsaSha384
    };
}
