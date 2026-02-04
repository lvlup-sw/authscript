using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Aggregates clinical data from FHIR API for prior authorization processing.
/// Token management is handled internally via IFhirTokenProvider.
/// </summary>
public interface IFhirDataAggregator
{
    /// <summary>
    /// Fetches and aggregates clinical data for a patient from the FHIR server.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="encounterId">Optional encounter ID to scope ServiceRequest queries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated clinical bundle with conditions, observations, procedures, and documents.</returns>
    /// <exception cref="HttpRequestException">When FHIR API is unreachable.</exception>
    Task<ClinicalBundle> AggregateClinicalDataAsync(
        string patientId,
        string? encounterId = null,
        CancellationToken cancellationToken = default);
}
