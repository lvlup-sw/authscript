// =============================================================================
// <copyright file="FhirEndpoints.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Filters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Gateway.API.Endpoints;

/// <summary>
/// Endpoints for FHIR discovery operations.
/// Provides direct access to query the FHIR sandbox for patients and encounters.
/// </summary>
public static class FhirEndpoints
{
    /// <summary>
    /// Maps FHIR discovery endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapFhirEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fhir")
            .WithTags("FHIR Discovery")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        group.MapGet("/patients", SearchPatientsAsync)
            .WithName("SearchPatients")
            .WithDescription("Search for patients by name in the FHIR sandbox");

        group.MapGet("/patients/{patientId}", GetPatientAsync)
            .WithName("GetFhirPatient")
            .WithDescription("Get a patient by ID from the FHIR sandbox");

        group.MapGet("/encounters", SearchEncountersAsync)
            .WithName("SearchEncounters")
            .WithDescription("Search for encounters by patient ID");
    }

    /// <summary>
    /// Searches for patients by name.
    /// </summary>
    /// <param name="name">The patient name to search for.</param>
    /// <param name="fhirClient">The FHIR HTTP client.</param>
    /// <param name="options">The Athena configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A FHIR Bundle containing matching patients.</returns>
    public static async Task<Results<Ok<JsonElement>, ProblemHttpResult>> SearchPatientsAsync(
        [FromQuery] string name,
        [FromServices] IFhirHttpClient fhirClient,
        [FromServices] IOptions<AthenaOptions> options,
        CancellationToken ct = default)
    {
        var query = BuildQuery($"name={name}", options.Value.PracticeId);
        var result = await fhirClient.SearchAsync("Patient", query, ct).ConfigureAwait(false);

        return result.Match<Results<Ok<JsonElement>, ProblemHttpResult>>(
            onSuccess: bundle => TypedResults.Ok(bundle),
            onFailure: error => TypedResults.Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: error.Code));
    }

    /// <summary>
    /// Gets a patient by ID.
    /// </summary>
    /// <param name="patientId">The patient ID.</param>
    /// <param name="fhirClient">The FHIR HTTP client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The patient resource or not found.</returns>
    public static async Task<Results<Ok<JsonElement>, NotFound, ProblemHttpResult>> GetPatientAsync(
        string patientId,
        [FromServices] IFhirHttpClient fhirClient,
        CancellationToken ct = default)
    {
        var result = await fhirClient.ReadAsync("Patient", patientId, ct).ConfigureAwait(false);

        return result.Match<Results<Ok<JsonElement>, NotFound, ProblemHttpResult>>(
            onSuccess: patient => TypedResults.Ok(patient),
            onFailure: error => error.Code == "NOT_FOUND"
                ? TypedResults.NotFound()
                : TypedResults.Problem(
                    detail: error.Message,
                    statusCode: StatusCodes.Status502BadGateway,
                    title: error.Code));
    }

    /// <summary>
    /// Searches for encounters by patient ID.
    /// </summary>
    /// <param name="patientId">The patient ID to search encounters for.</param>
    /// <param name="fhirClient">The FHIR HTTP client.</param>
    /// <param name="options">The Athena configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A FHIR Bundle containing matching encounters.</returns>
    public static async Task<Results<Ok<JsonElement>, ProblemHttpResult>> SearchEncountersAsync(
        [FromQuery] string patientId,
        [FromServices] IFhirHttpClient fhirClient,
        [FromServices] IOptions<AthenaOptions> options,
        CancellationToken ct = default)
    {
        var query = BuildQuery($"patient=Patient/{patientId}", options.Value.PracticeId);
        var result = await fhirClient.SearchAsync("Encounter", query, ct).ConfigureAwait(false);

        return result.Match<Results<Ok<JsonElement>, ProblemHttpResult>>(
            onSuccess: bundle => TypedResults.Ok(bundle),
            onFailure: error => TypedResults.Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: error.Code));
    }

    private static string BuildQuery(string baseQuery, string? practiceId)
    {
        return string.IsNullOrWhiteSpace(practiceId)
            ? baseQuery
            : $"{baseQuery}&ah-practice={practiceId}";
    }
}
