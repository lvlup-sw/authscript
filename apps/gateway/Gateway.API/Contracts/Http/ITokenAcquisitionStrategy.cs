namespace Gateway.API.Contracts.Http;

/// <summary>
/// Strategy interface for acquiring authentication tokens from different EHR providers.
/// </summary>
public interface ITokenAcquisitionStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle token acquisition for the current configuration.
    /// </summary>
    bool CanHandle { get; }

    /// <summary>
    /// Acquires an access token asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token, or null if acquisition fails.</returns>
    Task<string?> AcquireTokenAsync(CancellationToken cancellationToken = default);
}
