// =============================================================================
// <copyright file="Query.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.GraphQL.Models;
using Gateway.API.Services;

namespace Gateway.API.GraphQL.Queries;

/// <summary>
/// GraphQL query resolvers for PA request data and reference data.
/// </summary>
public sealed class Query
{
    /// <summary>Gets all available procedures.</summary>
    public IReadOnlyList<ProcedureModel> GetProcedures([Service] IDataService dataService) =>
        dataService.Procedures;

    /// <summary>Gets all available medications.</summary>
    public IReadOnlyList<MedicationModel> GetMedications([Service] IDataService dataService) =>
        dataService.Medications;

    /// <summary>Gets all available providers.</summary>
    public IReadOnlyList<ProviderModel> GetProviders([Service] IDataService dataService) =>
        dataService.Providers;

    /// <summary>Gets all available payers.</summary>
    public IReadOnlyList<PayerModel> GetPayers([Service] IDataService dataService) =>
        dataService.Payers;

    /// <summary>Gets all available diagnoses.</summary>
    public IReadOnlyList<DiagnosisModel> GetDiagnoses([Service] IDataService dataService) =>
        dataService.Diagnoses;

    /// <summary>Gets all PA requests.</summary>
    public IReadOnlyList<PARequestModel> GetPARequests([Service] IDataService dataService) =>
        dataService.GetPARequests();

    /// <summary>Gets a single PA request by ID.</summary>
    public PARequestModel? GetPARequest(string id, [Service] IDataService dataService) =>
        dataService.GetPARequest(id);

    /// <summary>Gets aggregated PA statistics.</summary>
    public PAStatsModel GetPAStats([Service] IDataService dataService) =>
        dataService.GetPAStats();

    /// <summary>Gets recent activity items.</summary>
    public IReadOnlyList<ActivityItemModel> GetActivity([Service] IDataService dataService) =>
        dataService.GetActivity();
}
