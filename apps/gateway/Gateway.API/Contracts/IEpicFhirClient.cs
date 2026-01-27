using Gateway.API.Abstractions;
using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Client for interacting with Epic's FHIR R4 API to retrieve clinical data.
/// Authentication is handled by the configured IHttpClientProvider.
/// </summary>
public interface IEpicFhirClient
{
    /// <summary>
    /// Retrieves patient demographic information.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing patient information or error.</returns>
    Task<Result<PatientInfo>> GetPatientAsync(
        string patientId,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for active conditions/diagnoses for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of active conditions or error.</returns>
    Task<Result<IReadOnlyList<ConditionInfo>>> SearchConditionsAsync(
        string patientId,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for clinical observations (labs, vitals) for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="since">Minimum date for observations to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of observations or error.</returns>
    Task<Result<IReadOnlyList<ObservationInfo>>> SearchObservationsAsync(
        string patientId,
        DateOnly since,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for procedures performed on a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="since">Minimum date for procedures to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of procedures or error.</returns>
    Task<Result<IReadOnlyList<ProcedureInfo>>> SearchProceduresAsync(
        string patientId,
        DateOnly since,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for clinical documents (notes, reports) for a patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of document references or error.</returns>
    Task<Result<IReadOnlyList<DocumentInfo>>> SearchDocumentsAsync(
        string patientId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the binary content of a document.
    /// </summary>
    /// <param name="documentId">The FHIR Binary resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing document bytes or error.</returns>
    Task<Result<byte[]>> GetDocumentContentAsync(
        string documentId,
        CancellationToken ct = default);
}
