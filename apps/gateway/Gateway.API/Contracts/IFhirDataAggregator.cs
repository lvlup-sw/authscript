using Gateway.API.Abstractions;
using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Aggregates clinical data from FHIR API for prior authorization processing.
/// Authentication is handled internally by IHttpClientProvider.
/// </summary>
public interface IFhirDataAggregator
{
    /// <summary>
    /// Fetches and aggregates clinical data for a patient from the FHIR server.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing aggregated clinical bundle or error.</returns>
    Task<Result<ClinicalBundle>> AggregateClinicalDataAsync(
        string patientId,
        CancellationToken ct = default);
}
