namespace Gateway.API.Contracts.Http;

/// <summary>
/// Provides authenticated HTTP clients for downstream services.
/// </summary>
public interface IHttpClientProvider
{
    /// <summary>
    /// Gets an HTTP client authenticated via client credentials flow.
    /// </summary>
    /// <param name="clientName">Named HttpClient to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated HttpClient or null if auth fails.</returns>
    Task<HttpClient?> GetAuthenticatedClientAsync(
        string clientName,
        CancellationToken cancellationToken = default);
}
