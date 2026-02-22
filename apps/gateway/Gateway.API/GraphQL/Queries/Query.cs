using Gateway.API.Contracts;
using Gateway.API.GraphQL.Models;
using Gateway.API.Services;

namespace Gateway.API.GraphQL.Queries;

public sealed class Query
{
    // Reference data (static, from ReferenceDataService)
    public IReadOnlyList<ProcedureModel> GetProcedures([Service] ReferenceDataService refData) =>
        refData.Procedures;

    public IReadOnlyList<MedicationModel> GetMedications([Service] ReferenceDataService refData) =>
        refData.Medications;

    public IReadOnlyList<ProviderModel> GetProviders([Service] ReferenceDataService refData) =>
        refData.Providers;

    public IReadOnlyList<PayerModel> GetPayers([Service] ReferenceDataService refData) =>
        refData.Payers;

    public IReadOnlyList<DiagnosisModel> GetDiagnoses([Service] ReferenceDataService refData) =>
        refData.Diagnoses;

    // PA request data (from PostgreSQL via IPARequestStore)
    public async Task<IReadOnlyList<PARequestModel>> GetPARequests(
        [Service] IPARequestStore store, CancellationToken ct) =>
        await store.GetAllAsync(ct);

    public async Task<PARequestModel?> GetPARequest(
        string id, [Service] IPARequestStore store, CancellationToken ct) =>
        await store.GetByIdAsync(id, ct);

    public async Task<PAStatsModel> GetPAStats(
        [Service] IPARequestStore store, CancellationToken ct) =>
        await store.GetStatsAsync(ct);

    public async Task<IReadOnlyList<ActivityItemModel>> GetActivity(
        [Service] IPARequestStore store, CancellationToken ct) =>
        await store.GetActivityAsync(ct);
}
