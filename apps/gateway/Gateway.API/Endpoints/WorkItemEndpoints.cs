using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Endpoints;

/// <summary>
/// Endpoints for managing prior authorization work items.
/// </summary>
public static class WorkItemEndpoints
{
    /// <summary>
    /// Maps the work item endpoints to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapWorkItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-items")
            .WithTags("WorkItems");

        group.MapPost("/{id}/rehydrate", RehydrateAsync)
            .WithName("RehydrateWorkItem")
            .WithSummary("Re-fetch clinical data and re-analyze work item")
            .Produces<RehydrateResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Re-fetches clinical data and re-analyzes a work item.
    /// </summary>
    /// <param name="id">The work item identifier.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="fhirAggregator">The FHIR data aggregator service.</param>
    /// <param name="intelligenceClient">The intelligence client service.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rehydrate response or 404 if not found.</returns>
    public static async Task<IResult> RehydrateAsync(
        string id,
        [FromServices] IWorkItemStore workItemStore,
        [FromServices] IFhirDataAggregator fhirAggregator,
        [FromServices] IIntelligenceClient intelligenceClient,
        [FromServices] ILogger<RehydrateResponse> logger,
        CancellationToken cancellationToken)
    {
        // 1. Get work item by ID
        var workItem = await workItemStore.GetByIdAsync(id, cancellationToken);
        if (workItem is null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Work item '{id}' not found",
                Code = "WORK_ITEM_NOT_FOUND"
            });
        }

        logger.LogInformation(
            "Rehydrating work item {WorkItemId} for patient {PatientId}",
            id, workItem.PatientId);

        // 2. Re-hydrate clinical data (using patient ID from work item)
        // MVP: Token management is handled by the polling service which stores tokens.
        // Production: Inject ITokenStrategyResolver to get valid token for the patient's practice.
        // See: https://github.com/anthropics/prior-auth/issues/19 for token provider implementation.
        var accessToken = "placeholder-token"; // TODO(#19): Replace with ITokenStrategyResolver

        var clinicalBundle = await fhirAggregator.AggregateClinicalDataAsync(
            workItem.PatientId,
            accessToken,
            cancellationToken);

        // 3. Re-analyze with intelligence service
        var analysisResult = await intelligenceClient.AnalyzeAsync(
            clinicalBundle,
            workItem.ProcedureCode,
            cancellationToken);

        // 4. Determine new status based on analysis
        var newStatus = DetermineStatus(analysisResult);

        // 5. Update work item status
        await workItemStore.UpdateStatusAsync(id, newStatus, cancellationToken);

        logger.LogInformation(
            "Work item {WorkItemId} rehydrated. New status: {NewStatus}",
            id, newStatus);

        return Results.Ok(new RehydrateResponse
        {
            WorkItemId = id,
            NewStatus = newStatus.ToString(),
            Message = "Work item rehydrated successfully"
        });
    }

    /// <summary>
    /// Determines the work item status based on analysis results.
    /// </summary>
    /// <param name="analysisResult">The PA form data from analysis.</param>
    /// <returns>The appropriate work item status.</returns>
    private static WorkItemStatus DetermineStatus(PAFormData analysisResult)
    {
        // If recommendation is "no_pa_required", set status accordingly
        if (analysisResult.Recommendation == "no_pa_required")
        {
            return WorkItemStatus.NoPaRequired;
        }

        // If confidence is high enough and recommendation is approve, ready for review
        if (analysisResult.ConfidenceScore >= 0.8 && analysisResult.Recommendation == "approve")
        {
            return WorkItemStatus.ReadyForReview;
        }

        // Otherwise, still missing data or needs more info
        return WorkItemStatus.MissingData;
    }
}
