namespace Gateway.API.Contracts.Fhir;

using Hl7.Fhir.Model;

/// <summary>
/// Repository pattern interface for FHIR resources.
/// Provides higher-level domain-oriented operations.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public interface IFhirRepository<TResource> where TResource : Resource
{
    /// <summary>
    /// Gets a resource by its ID.
    /// </summary>
    /// <param name="id">The resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resource or an error.</returns>
    Task<Result<TResource>> GetByIdAsync(string id, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Finds all resources for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resources for the patient or an error.</returns>
    Task<Result<IReadOnlyList<TResource>>> FindByPatientAsync(string patientId, string accessToken, CancellationToken ct = default);
}

/// <summary>
/// Extended repository interface with date-range filtering.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public interface IFhirRepositoryWithDateRange<TResource> : IFhirRepository<TResource> where TResource : Resource
{
    /// <summary>
    /// Finds resources for a patient within a date range.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="since">The start date (inclusive).</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resources within the date range or an error.</returns>
    Task<Result<IReadOnlyList<TResource>>> FindByPatientSinceAsync(
        string patientId,
        DateOnly since,
        string accessToken,
        CancellationToken ct = default);
}
