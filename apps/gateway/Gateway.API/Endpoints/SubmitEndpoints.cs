using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Endpoints;

/// <summary>
/// Endpoints for submitting PA forms to FHIR servers.
/// </summary>
public static class SubmitEndpoints
{
    /// <summary>
    /// Maps the submit endpoints to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapSubmitEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/submit/{transactionId}", async (
            string transactionId,
            [FromBody] SubmitRequest request,
            [FromServices] IDocumentUploader documentUploader,
            [FromServices] IAnalysisResultStore resultStore,
            [FromServices] IPdfFormStamper pdfStamper,
            CancellationToken ct) =>
        {
            return await SubmitAsync(transactionId, request, documentUploader, resultStore, pdfStamper, ct);
        })
        .WithName("SubmitPaForm")
        .WithTags("Submit")
        .WithSummary("Submit PA form to FHIR server")
        .Produces<SubmitResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Submits the PA form PDF to a FHIR server as a DocumentReference.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="request">The submission request with credentials.</param>
    /// <param name="documentUploader">The document uploader service.</param>
    /// <param name="resultStore">The analysis result store.</param>
    /// <param name="pdfStamper">The PDF stamper service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The submission response.</returns>
    public static async Task<IResult> SubmitAsync(
        string transactionId,
        SubmitRequest request,
        IDocumentUploader documentUploader,
        IAnalysisResultStore resultStore,
        IPdfFormStamper pdfStamper,
        CancellationToken ct)
    {
        // Try to get cached PDF first
        var pdfBytes = await resultStore.GetCachedPdfAsync(transactionId, ct);

        if (pdfBytes is null)
        {
            // No cached PDF, try to generate from form data
            var formData = await resultStore.GetCachedResponseAsync(transactionId, ct);

            if (formData is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"Transaction '{transactionId}' not found",
                    Code = "TRANSACTION_NOT_FOUND"
                });
            }

            // Generate PDF
            pdfBytes = await pdfStamper.StampFormAsync(formData, ct);
            await resultStore.SetCachedPdfAsync(transactionId, pdfBytes, ct);
        }

        // Upload to FHIR server
        var result = await documentUploader.UploadDocumentAsync(
            pdfBytes,
            request.PatientId,
            request.EncounterId,
            request.AccessToken,
            ct);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error?.Message,
                title: "FHIR Submission Failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new SubmitResponse
        {
            TransactionId = transactionId,
            Submitted = true,
            DocumentId = result.Value!,
            Message = "PA form successfully submitted to FHIR server"
        });
    }
}

/// <summary>
/// Request body for the submit endpoint.
/// </summary>
public sealed record SubmitRequest
{
    /// <summary>
    /// Gets the FHIR Patient resource ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the optional FHIR Encounter resource ID for context.
    /// </summary>
    public string? EncounterId { get; init; }

    /// <summary>
    /// Gets the OAuth access token for FHIR authentication.
    /// </summary>
    public required string AccessToken { get; init; }
}
