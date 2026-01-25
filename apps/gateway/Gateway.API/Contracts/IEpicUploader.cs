namespace Gateway.API.Contracts;

/// <summary>
/// Uploads completed PA forms to Epic as FHIR DocumentReference resources.
/// </summary>
public interface IEpicUploader
{
    /// <summary>
    /// Uploads a PDF document to Epic's FHIR server as a DocumentReference.
    /// </summary>
    /// <param name="pdfBytes">The PDF document content as a byte array.</param>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="encounterId">Optional FHIR Encounter resource ID for context.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The FHIR DocumentReference resource ID of the uploaded document.</returns>
    /// <exception cref="HttpRequestException">When the upload fails.</exception>
    Task<string> UploadDocumentAsync(
        byte[] pdfBytes,
        string patientId,
        string? encounterId,
        string accessToken,
        CancellationToken cancellationToken = default);
}
