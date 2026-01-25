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

        group.MapGet("/{transactionId}", GetAnalysis)
            .WithName("GetAnalysis")
            .WithSummary("Get analysis result by transaction ID")
            .Produces<AnalysisResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{transactionId}/status", GetAnalysisStatus)
            .WithName("GetAnalysisStatus")
            .WithSummary("Get current status of analysis")
            .Produces<StatusResponse>(StatusCodes.Status200OK);

        group.MapGet("/{transactionId}/form", DownloadForm)
            .WithName("DownloadForm")
            .WithSummary("Download the generated PA form PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{transactionId}/submit", SubmitToEpic)
            .WithName("SubmitToEpic")
            .WithSummary("Submit the PA form to Epic (manual fallback)")
            .Produces<SubmitResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", TriggerAnalysis)
            .WithName("TriggerAnalysis")
            .WithSummary("Manually trigger analysis (SMART app fallback)");
    }

    private static async Task<IResult> GetAnalysis(
        string transactionId,
        [FromServices] IDemoCacheService cacheService,
        CancellationToken cancellationToken)
    {
        return await GetAnalysisAsync(transactionId, cacheService, cancellationToken);
    }

    /// <summary>
    /// Gets the analysis result for a given transaction ID.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis response or 404 if not found.</returns>
    public static async Task<IResult> GetAnalysisAsync(
        string transactionId,
        IDemoCacheService cacheService,
        CancellationToken cancellationToken)
    {
        var formData = await cacheService.GetCachedResponseAsync(transactionId, cancellationToken);

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

    private static async Task<IResult> GetAnalysisStatus(
        string transactionId,
        [FromServices] IDemoCacheService cacheService,
        CancellationToken cancellationToken)
    {
        return await GetAnalysisStatusAsync(transactionId, cacheService, cancellationToken);
    }

    /// <summary>
    /// Gets the current status of an analysis.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The status response.</returns>
    public static async Task<IResult> GetAnalysisStatusAsync(
        string transactionId,
        IDemoCacheService cacheService,
        CancellationToken cancellationToken)
    {
        var formData = await cacheService.GetCachedResponseAsync(transactionId, cancellationToken);

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

    private static async Task<IResult> DownloadForm(
        string transactionId,
        [FromServices] IDemoCacheService cacheService,
        [FromServices] IPdfFormStamper pdfStamper,
        CancellationToken cancellationToken)
    {
        return await DownloadFormAsync(transactionId, cacheService, pdfStamper, cancellationToken);
    }

    /// <summary>
    /// Downloads the generated PA form PDF.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="pdfStamper">The PDF stamper service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PDF file or 404 if not found.</returns>
    public static async Task<IResult> DownloadFormAsync(
        string transactionId,
        IDemoCacheService cacheService,
        IPdfFormStamper pdfStamper,
        CancellationToken cancellationToken)
    {
        // First, try to get cached PDF
        var cachedPdf = await cacheService.GetCachedPdfAsync(transactionId, cancellationToken);

        if (cachedPdf is not null)
        {
            return Results.File(
                cachedPdf,
                "application/pdf",
                $"pa-form-{transactionId}.pdf");
        }

        // No cached PDF, try to generate from form data
        var formData = await cacheService.GetCachedResponseAsync(transactionId, cancellationToken);

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
        await cacheService.SetCachedPdfAsync(transactionId, pdfBytes, cancellationToken);

        return Results.File(
            pdfBytes,
            "application/pdf",
            $"pa-form-{transactionId}.pdf");
    }

    private static async Task<IResult> SubmitToEpic(
        string transactionId,
        [FromBody] SubmitToEpicRequest request,
        [FromServices] IEpicUploader epicUploader,
        [FromServices] IDemoCacheService cacheService,
        [FromServices] IPdfFormStamper pdfStamper,
        CancellationToken cancellationToken)
    {
        return await SubmitToEpicAsync(
            transactionId,
            request,
            epicUploader,
            cacheService,
            pdfStamper,
            cancellationToken);
    }

    /// <summary>
    /// Submits the PA form to Epic as a DocumentReference.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="request">The submission request with Epic credentials.</param>
    /// <param name="epicUploader">The Epic uploader service.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="pdfStamper">The PDF stamper service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The submission response.</returns>
    public static async Task<IResult> SubmitToEpicAsync(
        string transactionId,
        SubmitToEpicRequest request,
        IEpicUploader epicUploader,
        IDemoCacheService cacheService,
        IPdfFormStamper pdfStamper,
        CancellationToken cancellationToken)
    {
        // Get the PDF (from cache or generate)
        var pdfBytes = await cacheService.GetCachedPdfAsync(transactionId, cancellationToken);

        if (pdfBytes is null)
        {
            var formData = await cacheService.GetCachedResponseAsync(transactionId, cancellationToken);

            if (formData is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"No analysis data found for transaction '{transactionId}'",
                    Code = "ANALYSIS_NOT_FOUND"
                });
            }

            pdfBytes = await pdfStamper.StampFormAsync(formData, cancellationToken);
            await cacheService.SetCachedPdfAsync(transactionId, pdfBytes, cancellationToken);
        }

        try
        {
            var documentId = await epicUploader.UploadDocumentAsync(
                pdfBytes,
                request.PatientId,
                request.EncounterId,
                request.AccessToken,
                cancellationToken);

            return Results.Ok(new SubmitResponse
            {
                TransactionId = transactionId,
                Submitted = true,
                DocumentId = documentId,
                Message = "PA form successfully submitted to Epic"
            });
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Epic Submission Failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> TriggerAnalysis(
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
        return Results.Accepted(value: new
        {
            transactionId,
            status = "queued",
            message = "Analysis queued for processing"
        });
    }

    /// <summary>
    /// Request for manually triggering an analysis.
    /// </summary>
    public sealed record ManualAnalysisRequest
    {
        /// <summary>
        /// Gets the FHIR Patient resource ID.
        /// </summary>
        public required string PatientId { get; init; }

        /// <summary>
        /// Gets the procedure code (CPT) to analyze.
        /// </summary>
        public required string ProcedureCode { get; init; }

        /// <summary>
        /// Gets the optional FHIR Encounter resource ID.
        /// </summary>
        public string? EncounterId { get; init; }

        /// <summary>
        /// Gets the OAuth access token for Epic FHIR access.
        /// </summary>
        public required string AccessToken { get; init; }
    }
}
