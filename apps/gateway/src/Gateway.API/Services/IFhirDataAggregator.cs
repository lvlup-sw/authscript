using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Aggregates clinical data from FHIR API for prior authorization processing.
/// </summary>
public interface IFhirDataAggregator
{
    /// <summary>
    /// Fetches and aggregates clinical data for a patient from the FHIR server.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="accessToken">OAuth access token for FHIR API calls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated clinical bundle with conditions, observations, procedures, and documents.</returns>
    /// <exception cref="HttpRequestException">When FHIR API is unreachable.</exception>
    Task<ClinicalBundle> AggregateClinicalDataAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default);
}
