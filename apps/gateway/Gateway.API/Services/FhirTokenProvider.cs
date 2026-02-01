using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Microsoft.Extensions.Logging;

namespace Gateway.API.Services;

/// <summary>
/// Provides access tokens for FHIR API calls using the configured token acquisition strategy.
/// </summary>
public sealed class FhirTokenProvider : IFhirTokenProvider
{
    private readonly ITokenStrategyResolver _resolver;
    private readonly ILogger<FhirTokenProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirTokenProvider"/> class.
    /// </summary>
    /// <param name="resolver">The token strategy resolver.</param>
    /// <param name="logger">Logger instance.</param>
    public FhirTokenProvider(ITokenStrategyResolver resolver, ILogger<FhirTokenProvider> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var strategy = _resolver.Resolve();
        var token = await strategy.AcquireTokenAsync(cancellationToken);

        if (token is null)
        {
            _logger.LogError("Failed to acquire FHIR access token");
            throw new InvalidOperationException("Unable to acquire FHIR access token");
        }

        return token;
    }
}
