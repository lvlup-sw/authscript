// =============================================================================
// <copyright file="LiveDataService.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.GraphQL.Models;

namespace Gateway.API.Services;

/// <summary>
/// Live data service skeleton that will connect to real data sources.
/// All methods currently throw <see cref="NotImplementedException"/>
/// until integrated with persistence layer.
/// </summary>
public sealed class LiveDataService : IDataService
{
    /// <inheritdoc />
    public IReadOnlyList<ProcedureModel> Procedures =>
        throw new NotImplementedException("LiveDataService.Procedures not yet implemented.");

    /// <inheritdoc />
    public IReadOnlyList<MedicationModel> Medications =>
        throw new NotImplementedException("LiveDataService.Medications not yet implemented.");

    /// <inheritdoc />
    public IReadOnlyList<ProviderModel> Providers =>
        throw new NotImplementedException("LiveDataService.Providers not yet implemented.");

    /// <inheritdoc />
    public IReadOnlyList<PayerModel> Payers =>
        throw new NotImplementedException("LiveDataService.Payers not yet implemented.");

    /// <inheritdoc />
    public IReadOnlyList<DiagnosisModel> Diagnoses =>
        throw new NotImplementedException("LiveDataService.Diagnoses not yet implemented.");

    /// <inheritdoc />
    public IReadOnlyList<PARequestModel> GetPARequests() =>
        throw new NotImplementedException("LiveDataService.GetPARequests not yet implemented.");

    /// <inheritdoc />
    public PARequestModel? GetPARequest(string id) =>
        throw new NotImplementedException("LiveDataService.GetPARequest not yet implemented.");

    /// <inheritdoc />
    public PAStatsModel GetPAStats() =>
        throw new NotImplementedException("LiveDataService.GetPAStats not yet implemented.");

    /// <inheritdoc />
    public IReadOnlyList<ActivityItemModel> GetActivity() =>
        throw new NotImplementedException("LiveDataService.GetActivity not yet implemented.");

    /// <inheritdoc />
    public PARequestModel CreatePARequest(PatientModel patient, string procedureCode, string diagnosisCode, string diagnosisName, string providerId = "DR001") =>
        throw new NotImplementedException("LiveDataService.CreatePARequest not yet implemented.");

    /// <inheritdoc />
    public PARequestModel? UpdatePARequest(string id, string? diagnosis, string? diagnosisCode, string? serviceDate, string? placeOfService, string? clinicalSummary, IReadOnlyList<CriterionModel>? criteria) =>
        throw new NotImplementedException("LiveDataService.UpdatePARequest not yet implemented.");

    /// <inheritdoc />
    public Task<PARequestModel?> ProcessPARequestAsync(string id, CancellationToken ct = default) =>
        throw new NotImplementedException("LiveDataService.ProcessPARequestAsync not yet implemented.");

    /// <inheritdoc />
    public PARequestModel? SubmitPARequest(string id, int addReviewTimeSeconds = 0) =>
        throw new NotImplementedException("LiveDataService.SubmitPARequest not yet implemented.");

    /// <inheritdoc />
    public PARequestModel? AddReviewTime(string id, int seconds) =>
        throw new NotImplementedException("LiveDataService.AddReviewTime not yet implemented.");

    /// <inheritdoc />
    public bool DeletePARequest(string id) =>
        throw new NotImplementedException("LiveDataService.DeletePARequest not yet implemented.");

    /// <inheritdoc />
    public PARequestModel? ApprovePA(string id) =>
        throw new NotImplementedException("LiveDataService.ApprovePA not yet implemented.");

    /// <inheritdoc />
    public PARequestModel? DenyPA(string id, string reason) =>
        throw new NotImplementedException("LiveDataService.DenyPA not yet implemented.");
}
