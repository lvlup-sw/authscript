using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// High-level client for FHIR R4 API operations.
/// Provides domain-specific methods for retrieving clinical data.
/// </summary>
public interface IFhirClient
{
    /// <summary>
    /// Retrieves patient demographic information.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Patient information or null if not found.</returns>
    Task<PatientInfo?> GetPatientAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for active conditions/diagnoses for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active conditions.</returns>
    Task<List<ConditionInfo>> SearchConditionsAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for clinical observations (labs, vitals) for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="since">Minimum date for observations to include.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of observations since the specified date.</returns>
    Task<List<ObservationInfo>> SearchObservationsAsync(
        string patientId,
        DateOnly since,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for procedures performed on a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="since">Minimum date for procedures to include.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of procedures since the specified date.</returns>
    Task<List<ProcedureInfo>> SearchProceduresAsync(
        string patientId,
        DateOnly since,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for clinical documents (notes, reports) for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document references.</returns>
    Task<List<DocumentInfo>> SearchDocumentsAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the binary content of a document.
    /// </summary>
    /// <param name="documentId">The FHIR Binary resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document content as byte array or null if not found.</returns>
    Task<byte[]?> GetDocumentContentAsync(
        string documentId,
        string accessToken,
        CancellationToken cancellationToken = default);
}
