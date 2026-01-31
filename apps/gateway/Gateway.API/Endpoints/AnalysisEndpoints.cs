using Gateway.API.Contracts;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Endpoints;

/// <summary>
/// Endpoints for retrieving and managing prior authorization analysis results.
/// </summary>
public static class AnalysisEndpoints
{
    /// <summary>
    /// Maps the analysis endpoints to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analysis")
            .WithTags("Analysis");

        group.MapGet("/{transactionId}", GetAnalysisAsync)
            .WithName("GetAnalysis")
            .WithSummary("Get analysis result by transaction ID")
            .Produces<AnalysisResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{transactionId}/status", GetAnalysisStatusAsync)
            .WithName("GetAnalysisStatus")
            .WithSummary("Get current status of analysis")
            .Produces<StatusResponse>(StatusCodes.Status200OK);

        group.MapGet("/{transactionId}/form", DownloadFormAsync)
            .WithName("DownloadForm")
            .WithSummary("Download the generated PA form PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{transactionId}/submit", SubmitToFhirAsync)
            .WithName("SubmitToFhir")
            .WithSummary("Submit the PA form to FHIR server (manual fallback)")
            .Produces<SubmitResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", TriggerAnalysisAsync)
            .WithName("TriggerAnalysis")
            .WithSummary("Manually trigger analysis (SMART app fallback)");
    }

    /// <summary>
    /// Gets the analysis result for a given transaction ID.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="resultStore">The analysis result store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis response or 404 if not found.</returns>
    public static async Task<IResult> GetAnalysisAsync(
        string transactionId,
        [FromServices] IAnalysisResultStore resultStore,
        CancellationToken cancellationToken)
    {
        var formData = await resultStore.GetCachedResponseAsync(transactionId, cancellationToken);

        if (formData is null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Analysis with transaction ID '{transactionId}' not found",
                Code = "ANALYSIS_NOT_FOUND"
            });
        }

        return Results.Ok(new AnalysisResponse
        {
            TransactionId = transactionId,
            Status = "completed",
            FormData = formData,
            Message = "Analysis complete"
        });
    }

    /// <summary>
    /// Gets the current status of an analysis.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="resultStore">The analysis result store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The status response.</returns>
    public static async Task<IResult> GetAnalysisStatusAsync(
        string transactionId,
        [FromServices] IAnalysisResultStore resultStore,
        CancellationToken cancellationToken)
    {
        var formData = await resultStore.GetCachedResponseAsync(transactionId, cancellationToken);

        if (formData is not null)
        {
            return Results.Ok(new StatusResponse
            {
                TransactionId = transactionId,
                Step = "completed",
                Message = "Analysis complete. Form ready for download.",
                Progress = 100
            });
        }

        // When no data exists, assume it's still processing
        return Results.Ok(new StatusResponse
        {
            TransactionId = transactionId,
            Step = "in_progress",
            Message = "Analysis in progress. Please check back later.",
            Progress = 50
        });
    }

    /// <summary>
    /// Downloads the generated PA form PDF.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="resultStore">The analysis result store.</param>
    /// <param name="pdfStamper">The PDF stamper service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PDF file or 404 if not found.</returns>
    public static async Task<IResult> DownloadFormAsync(
        string transactionId,
        [FromServices] IAnalysisResultStore resultStore,
        [FromServices] IPdfFormStamper pdfStamper,
        CancellationToken cancellationToken)
    {
        // First, try to get cached PDF
        var cachedPdf = await resultStore.GetCachedPdfAsync(transactionId, cancellationToken);

        if (cachedPdf is not null)
        {
            return Results.File(
                cachedPdf,
                "application/pdf",
                $"pa-form-{transactionId}.pdf");
        }

        // No cached PDF, try to generate from form data
        var formData = await resultStore.GetCachedResponseAsync(transactionId, cancellationToken);

        if (formData is null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"No analysis data found for transaction '{transactionId}'",
                Code = "FORM_NOT_FOUND"
            });
        }

        // Generate PDF and cache it
        var pdfBytes = await pdfStamper.StampFormAsync(formData, cancellationToken);
        await resultStore.SetCachedPdfAsync(transactionId, pdfBytes, cancellationToken);

        return Results.File(
            pdfBytes,
            "application/pdf",
            $"pa-form-{transactionId}.pdf");
    }

    /// <summary>
    /// Submits the PA form to FHIR server as a DocumentReference.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="request">The submission request with credentials.</param>
    /// <param name="documentUploader">The document uploader service.</param>
    /// <param name="resultStore">The analysis result store.</param>
    /// <param name="pdfStamper">The PDF stamper service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The submission response.</returns>
    public static async Task<IResult> SubmitToFhirAsync(
        string transactionId,
        [FromBody] SubmitToFhirRequest request,
        [FromServices] IDocumentUploader documentUploader,
        [FromServices] IAnalysisResultStore resultStore,
        [FromServices] IPdfFormStamper pdfStamper,
        CancellationToken cancellationToken)
    {
        // Get the PDF (from cache or generate)
        var pdfBytes = await resultStore.GetCachedPdfAsync(transactionId, cancellationToken);

        if (pdfBytes is null)
        {
            var formData = await resultStore.GetCachedResponseAsync(transactionId, cancellationToken);

            if (formData is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"No analysis data found for transaction '{transactionId}'",
                    Code = "ANALYSIS_NOT_FOUND"
                });
            }

            pdfBytes = await pdfStamper.StampFormAsync(formData, cancellationToken);
            await resultStore.SetCachedPdfAsync(transactionId, pdfBytes, cancellationToken);
        }

        var result = await documentUploader.UploadDocumentAsync(
            pdfBytes,
            request.PatientId,
            request.EncounterId,
            request.AccessToken,
            cancellationToken);

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

    /// <summary>
    /// Triggers a manual analysis for the given patient and procedure.
    /// </summary>
    /// <param name="request">The analysis request with patient and procedure details.</param>
    /// <param name="fhirAggregator">The FHIR data aggregator service.</param>
    /// <param name="intelligenceClient">The intelligence client service.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The queued analysis response with transaction ID.</returns>
    public static Task<IResult> TriggerAnalysisAsync(
        [FromBody] ManualAnalysisRequest request,
        [FromServices] IFhirDataAggregator fhirAggregator,
        [FromServices] IIntelligenceClient intelligenceClient,
        [FromServices] ILogger<ManualAnalysisRequest> logger,
        CancellationToken cancellationToken)
    {
        var transactionId = $"txn-{Guid.NewGuid():N}";

        logger.LogInformation(
            "Manual analysis triggered. TransactionId={TransactionId}, PatientId={PatientId}",
            transactionId, request.PatientId);

        // In production, this would queue the analysis job
        return Task.FromResult(Results.Accepted(value: new
        {
            transactionId,
            status = "queued",
            message = "Analysis queued for processing"
        }));
    }
}
