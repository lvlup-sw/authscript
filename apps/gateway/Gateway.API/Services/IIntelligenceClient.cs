using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Client for the Intelligence service that analyzes clinical data
/// and generates prior authorization form data.
/// </summary>
public interface IIntelligenceClient
{
    /// <summary>
    /// Analyzes clinical data against payer policies and generates PA form data.
    /// </summary>
    /// <param name="clinicalBundle">Aggregated clinical data for the patient.</param>
    /// <param name="procedureCode">The CPT procedure code being requested.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Prior authorization form data with AI recommendation and field mappings.</returns>
    /// <exception cref="HttpRequestException">When the Intelligence service is unreachable.</exception>
    /// <exception cref="InvalidOperationException">When the service returns an invalid response.</exception>
    Task<PAFormData> AnalyzeAsync(
        ClinicalBundle clinicalBundle,
        string procedureCode,
        CancellationToken cancellationToken = default);
}
