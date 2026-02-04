namespace Gateway.API.Contracts.Http;

/// <summary>
/// Provides authenticated HTTP clients for Epic FHIR API.
/// </summary>
public interface IFhirHttpClientProvider
{
    /// <summary>
    /// Gets an HTTP client with Bearer token attached.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An HTTP client configured with authentication.</returns>
    Task<HttpClient> GetAuthenticatedClientAsync(CancellationToken ct = default);
}
