using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Caching service for demo mode to reduce redundant Intelligence service calls.
/// </summary>
public interface IDemoCacheService
{
    /// <summary>
    /// Retrieves a cached PA form data response.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically patient + procedure combination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached form data or null if not found.</returns>
    Task<PAFormData?> GetCachedResponseAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a PA form data response.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically patient + procedure combination).</param>
    /// <param name="formData">The form data to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetCachedResponseAsync(string cacheKey, PAFormData formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a cached PDF document.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically patient + procedure combination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached PDF bytes or null if not found.</returns>
    Task<byte[]?> GetCachedPdfAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a PDF document.
    /// </summary>
    /// <param name="cacheKey">The cache key (typically patient + procedure combination).</param>
    /// <param name="pdfBytes">The PDF bytes to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetCachedPdfAsync(string cacheKey, byte[] pdfBytes, CancellationToken cancellationToken = default);
}
