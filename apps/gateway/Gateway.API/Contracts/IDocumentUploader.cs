namespace Gateway.API.Contracts;

/// <summary>
/// Interface for uploading documents to a FHIR server.
/// Token management is handled internally via IFhirTokenProvider.
/// </summary>
public interface IDocumentUploader
{
    /// <summary>
    /// Uploads a PDF document as a FHIR DocumentReference resource.
    /// </summary>
    /// <param name="pdfBytes">The PDF content to upload.</param>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="encounterId">Optional FHIR Encounter resource ID for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created DocumentReference resource ID, or an error.</returns>
    Task<Result<string>> UploadDocumentAsync(
        byte[] pdfBytes,
        string patientId,
        string? encounterId,
        CancellationToken cancellationToken = default);
}
