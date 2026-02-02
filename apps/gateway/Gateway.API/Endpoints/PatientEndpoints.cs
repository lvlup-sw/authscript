// =============================================================================
// <copyright file="PatientEndpoints.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Endpoints;

/// <summary>
/// Endpoints for patient registration and management.
/// </summary>
public static class PatientEndpoints
{
    /// <summary>
    /// Maps patient-related endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients")
            .WithTags("Patients");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterPatient")
            .WithDescription("Register a patient for encounter monitoring");

        group.MapDelete("/{patientId}", UnregisterAsync)
            .WithName("UnregisterPatient")
            .WithDescription("Unregister a patient from encounter monitoring");

        group.MapGet("/{patientId}", GetAsync)
            .WithName("GetPatient")
            .WithDescription("Get patient registration status");
    }

    /// <summary>
    /// Registers a patient for encounter monitoring and creates a work item.
    /// </summary>
    /// <param name="request">The patient registration request.</param>
    /// <param name="workItemStore">The work item store service.</param>
    /// <param name="patientRegistry">The patient registry service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registration response containing the work item ID.</returns>
    public static async Task<Results<Ok<RegisterPatientResponse>, BadRequest<string>>> RegisterAsync(
        [FromBody] RegisterPatientRequest request,
        [FromServices] IWorkItemStore workItemStore,
        [FromServices] IPatientRegistry patientRegistry,
        CancellationToken ct = default)
    {
        // 1. Create work item in Pending status
        var workItem = new WorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            PatientId = request.PatientId,
            EncounterId = request.EncounterId,
            Status = WorkItemStatus.Pending,
            ServiceRequestId = null,
            ProcedureCode = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var workItemId = await workItemStore.CreateAsync(workItem, ct).ConfigureAwait(false);

        // 2. Register patient with work item ID
        var registeredPatient = new RegisteredPatient
        {
            PatientId = request.PatientId,
            EncounterId = request.EncounterId,
            PracticeId = request.PracticeId,
            WorkItemId = workItemId,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        await patientRegistry.RegisterAsync(registeredPatient, ct).ConfigureAwait(false);

        // 3. Return response
        return TypedResults.Ok(new RegisterPatientResponse
        {
            WorkItemId = workItemId,
            Message = $"Patient {request.PatientId} registered for encounter monitoring"
        });
    }

    /// <summary>
    /// Unregisters a patient from encounter monitoring.
    /// </summary>
    /// <param name="patientId">The patient ID to unregister.</param>
    /// <param name="patientRegistry">The patient registry service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An OK result (idempotent - returns OK even if patient doesn't exist).</returns>
    public static async Task<Results<Ok, NotFound>> UnregisterAsync(
        string patientId,
        [FromServices] IPatientRegistry patientRegistry,
        CancellationToken ct = default)
    {
        await patientRegistry.UnregisterAsync(patientId, ct).ConfigureAwait(false);
        return TypedResults.Ok();
    }

    /// <summary>
    /// Gets a patient's registration status.
    /// </summary>
    /// <param name="patientId">The patient ID to look up.</param>
    /// <param name="patientRegistry">The patient registry service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registered patient if found; otherwise, NotFound.</returns>
    public static async Task<Results<Ok<RegisteredPatient>, NotFound>> GetAsync(
        string patientId,
        [FromServices] IPatientRegistry patientRegistry,
        CancellationToken ct = default)
    {
        var patient = await patientRegistry.GetAsync(patientId, ct).ConfigureAwait(false);
        return patient is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(patient);
    }
}
