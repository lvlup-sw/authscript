namespace Gateway.API.Contracts;

/// <summary>
/// Provides access tokens for FHIR API calls.
/// </summary>
public interface IFhirTokenProvider
{
    /// <summary>
    /// Gets a valid access token for FHIR API calls.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A valid access token.</returns>
    /// <exception cref="InvalidOperationException">Unable to acquire token.</exception>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
