using Gateway.API.Abstractions;

namespace Gateway.API.Contracts;

/// <summary>
/// Uploads completed PA forms to Epic as FHIR DocumentReference resources.
/// Authentication is handled internally by IHttpClientProvider.
/// </summary>
public interface IEpicUploader
{
    /// <summary>
    /// Uploads a PDF document to Epic's FHIR server as a DocumentReference.
    /// </summary>
    /// <param name="pdfBytes">The PDF document content as a byte array.</param>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="encounterId">Optional FHIR Encounter resource ID for context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the uploaded DocumentReference ID or error.</returns>
    Task<Result<string>> UploadDocumentAsync(
        byte[] pdfBytes,
        string patientId,
        string? encounterId,
        CancellationToken ct = default);
}
