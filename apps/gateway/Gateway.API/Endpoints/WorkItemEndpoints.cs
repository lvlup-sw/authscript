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

        // CREATE
        group.MapPost("/", CreateAsync)
            .WithName("CreateWorkItem")
            .WithSummary("Create a new work item")
            .Produces<WorkItem>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // LIST
        group.MapGet("/", ListAsync)
            .WithName("ListWorkItems")
            .WithSummary("List all work items")
            .Produces<WorkItemListResponse>(StatusCodes.Status200OK);

        // GET BY ID
        group.MapGet("/{id}", GetByIdAsync)
            .WithName("GetWorkItem")
            .WithSummary("Get work item by ID")
            .Produces<WorkItem>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // UPDATE STATUS
        group.MapPut("/{id}/status", UpdateStatusAsync)
            .WithName("UpdateWorkItemStatus")
            .WithSummary("Update work item status")
            .Produces<WorkItem>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // REHYDRATE
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
    /// <param name="request">Optional request body containing access token.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="fhirAggregator">The FHIR data aggregator service.</param>
    /// <param name="intelligenceClient">The intelligence client service.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rehydrate response or 404 if not found.</returns>
    public static async Task<IResult> RehydrateAsync(
        string id,
        [FromBody] RehydrateRequest? request,
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

        // 2. Use provided access token or fall back to placeholder
        // MVP: Token management is handled by the polling service which stores tokens.
        // Production: Inject ITokenStrategyResolver to get valid token for the patient's practice.
        // See: https://github.com/anthropics/prior-auth/issues/19 for token provider implementation.
        var accessToken = request?.AccessToken ?? "placeholder-token";

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
        var updated = await workItemStore.UpdateStatusAsync(id, newStatus, cancellationToken);
        if (!updated)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Work item '{id}' not found or update failed",
                Code = "WORK_ITEM_UPDATE_FAILED"
            });
        }

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

    /// <summary>
    /// Creates a new work item.
    /// </summary>
    /// <param name="request">The create work item request.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created work item with 201 status.</returns>
    public static async Task<IResult> CreateAsync(
        [FromBody] CreateWorkItemRequest request,
        [FromServices] IWorkItemStore workItemStore,
        [FromServices] ILogger<WorkItem> logger,
        CancellationToken cancellationToken)
    {
        var workItem = new WorkItem
        {
            Id = $"wi-{Guid.NewGuid():N}",
            EncounterId = request.EncounterId,
            PatientId = request.PatientId,
            ServiceRequestId = request.ServiceRequestId,
            ProcedureCode = request.ProcedureCode,
            Status = request.Status ?? WorkItemStatus.MissingData,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await workItemStore.CreateAsync(workItem, cancellationToken);

        logger.LogInformation("Created work item {WorkItemId} for encounter {EncounterId}",
            workItem.Id, workItem.EncounterId);

        return Results.Created($"/api/work-items/{workItem.Id}", workItem);
    }

    /// <summary>
    /// Lists all work items with optional filters.
    /// </summary>
    /// <param name="encounterId">Optional encounter ID filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching work items.</returns>
    public static async Task<IResult> ListAsync(
        [FromQuery] string? encounterId,
        [FromQuery] WorkItemStatus? status,
        [FromServices] IWorkItemStore workItemStore,
        CancellationToken cancellationToken)
    {
        var items = await workItemStore.GetAllAsync(encounterId, status, cancellationToken);

        return Results.Ok(new WorkItemListResponse
        {
            Items = items,
            Total = items.Count
        });
    }

    /// <summary>
    /// Gets a work item by ID.
    /// </summary>
    /// <param name="id">The work item identifier.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The work item if found, 404 otherwise.</returns>
    public static async Task<IResult> GetByIdAsync(
        string id,
        [FromServices] IWorkItemStore workItemStore,
        CancellationToken cancellationToken)
    {
        var workItem = await workItemStore.GetByIdAsync(id, cancellationToken);
        if (workItem is null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Work item '{id}' not found",
                Code = "WORK_ITEM_NOT_FOUND"
            });
        }

        return Results.Ok(workItem);
    }

    /// <summary>
    /// Updates a work item's status.
    /// </summary>
    /// <param name="id">The work item identifier.</param>
    /// <param name="request">The update status request.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated work item if found, 404 otherwise.</returns>
    public static async Task<IResult> UpdateStatusAsync(
        string id,
        [FromBody] UpdateStatusRequest request,
        [FromServices] IWorkItemStore workItemStore,
        [FromServices] ILogger<WorkItem> logger,
        CancellationToken cancellationToken)
    {
        var workItem = await workItemStore.GetByIdAsync(id, cancellationToken);
        if (workItem is null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Work item '{id}' not found",
                Code = "WORK_ITEM_NOT_FOUND"
            });
        }

        var success = await workItemStore.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (!success)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Work item '{id}' was modified or deleted",
                Code = "WORK_ITEM_UPDATE_FAILED"
            });
        }

        logger.LogInformation("Updated work item {WorkItemId} status to {Status}",
            id, request.Status);

        // Return updated work item
        var updatedItem = await workItemStore.GetByIdAsync(id, cancellationToken);
        if (updatedItem is null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Message = $"Work item '{id}' not found after update",
                Code = "WORK_ITEM_NOT_FOUND"
            });
        }

        return Results.Ok(updatedItem);
    }
}
