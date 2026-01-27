using Gateway.API.Abstractions;
using Gateway.API.Models;

namespace Gateway.API.Contracts;

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
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing PA form data or error.</returns>
    Task<Result<PAFormData>> AnalyzeAsync(
        ClinicalBundle clinicalBundle,
        string procedureCode,
        CancellationToken ct = default);
}
