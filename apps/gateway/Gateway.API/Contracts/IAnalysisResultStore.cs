using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Stores and retrieves completed analysis results.
/// Used for caching analysis responses and generated PDFs by transaction ID.
/// </summary>
public interface IAnalysisResultStore
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
