// =============================================================================
// <copyright file="IDataService.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.GraphQL.Models;

namespace Gateway.API.Services;

/// <summary>
/// Abstraction over the data layer for PA requests, reference data, and workflow operations.
/// </summary>
public interface IDataService
{
    /// <summary>Gets all prior authorization requests.</summary>
    IReadOnlyList<PARequestModel> GetPARequests();

    /// <summary>Gets a single prior authorization request by ID.</summary>
    /// <param name="id">The PA request identifier.</param>
    PARequestModel? GetPARequest(string id);

    /// <summary>Gets aggregated PA statistics.</summary>
    PAStatsModel GetPAStats();

    /// <summary>Gets recent activity items.</summary>
    IReadOnlyList<ActivityItemModel> GetActivity();

    /// <summary>Gets the list of available procedures.</summary>
    IReadOnlyList<ProcedureModel> Procedures { get; }

    /// <summary>Gets the list of available medications.</summary>
    IReadOnlyList<MedicationModel> Medications { get; }

    /// <summary>Gets the list of available providers.</summary>
    IReadOnlyList<ProviderModel> Providers { get; }

    /// <summary>Gets the list of available payers.</summary>
    IReadOnlyList<PayerModel> Payers { get; }

    /// <summary>Gets the list of available diagnoses.</summary>
    IReadOnlyList<DiagnosisModel> Diagnoses { get; }

    /// <summary>Creates a new prior authorization request.</summary>
    /// <param name="patient">The patient model.</param>
    /// <param name="procedureCode">The procedure or medication code.</param>
    /// <param name="diagnosisCode">The ICD-10 diagnosis code.</param>
    /// <param name="diagnosisName">The human-readable diagnosis name.</param>
    /// <param name="providerId">The provider identifier (defaults to DR001).</param>
    PARequestModel CreatePARequest(PatientModel patient, string procedureCode, string diagnosisCode, string diagnosisName, string providerId = "DR001");

    /// <summary>Updates an existing prior authorization request.</summary>
    /// <param name="id">The PA request identifier.</param>
    /// <param name="diagnosis">Updated diagnosis name.</param>
    /// <param name="diagnosisCode">Updated diagnosis code.</param>
    /// <param name="serviceDate">Updated service date.</param>
    /// <param name="placeOfService">Updated place of service.</param>
    /// <param name="clinicalSummary">Updated clinical summary.</param>
    /// <param name="criteria">Updated criteria list.</param>
    PARequestModel? UpdatePARequest(string id, string? diagnosis, string? diagnosisCode, string? serviceDate, string? placeOfService, string? clinicalSummary, IReadOnlyList<CriterionModel>? criteria);

    /// <summary>Processes a PA request through AI analysis (simulated).</summary>
    /// <param name="id">The PA request identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PARequestModel?> ProcessPARequestAsync(string id, CancellationToken ct = default);

    /// <summary>Submits a PA request for insurance review.</summary>
    /// <param name="id">The PA request identifier.</param>
    /// <param name="addReviewTimeSeconds">Additional review time in seconds.</param>
    PARequestModel? SubmitPARequest(string id, int addReviewTimeSeconds = 0);

    /// <summary>Adds review time to a PA request.</summary>
    /// <param name="id">The PA request identifier.</param>
    /// <param name="seconds">Seconds to add.</param>
    PARequestModel? AddReviewTime(string id, int seconds);

    /// <summary>Deletes a PA request.</summary>
    /// <param name="id">The PA request identifier.</param>
    bool DeletePARequest(string id);

    /// <summary>Approves a PA request that is waiting for insurance.</summary>
    /// <param name="id">The PA request identifier.</param>
    /// <returns>The updated model, or null if the request was not found or not in the correct status.</returns>
    PARequestModel? ApprovePA(string id);

    /// <summary>Denies a PA request that is waiting for insurance.</summary>
    /// <param name="id">The PA request identifier.</param>
    /// <param name="reason">The denial reason.</param>
    /// <returns>The updated model, or null if the request was not found or not in the correct status.</returns>
    PARequestModel? DenyPA(string id, string reason);
}
