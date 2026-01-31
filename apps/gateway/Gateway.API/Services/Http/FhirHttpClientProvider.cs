using System.Net.Http.Headers;
using Gateway.API.Contracts.Http;

namespace Gateway.API.Services.Http;

/// <summary>
/// Provides HTTP clients authenticated for Epic FHIR API access.
/// </summary>
public sealed class FhirHttpClientProvider : IFhirHttpClientProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenStrategyResolver _tokenResolver;
    private readonly ILogger<FhirHttpClientProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirHttpClientProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="tokenResolver">Resolver for token acquisition strategies.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirHttpClientProvider(
        IHttpClientFactory httpClientFactory,
        ITokenStrategyResolver tokenResolver,
        ILogger<FhirHttpClientProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenResolver = tokenResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HttpClient> GetAuthenticatedClientAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("EpicFhir");

        var strategy = _tokenResolver.Resolve();
        var token = await strategy.AcquireTokenAsync(ct);

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("No access token acquired for FHIR request");
        }

        return client;
    }
}
