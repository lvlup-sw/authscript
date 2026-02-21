using Gateway.API.GraphQL.Models;
using Gateway.API.Services;

namespace Gateway.API.GraphQL.Queries;

public sealed class Query
{
    public IReadOnlyList<ProcedureModel> GetProcedures([Service] MockDataService mockData) =>
        mockData.Procedures;

    public IReadOnlyList<MedicationModel> GetMedications([Service] MockDataService mockData) =>
        mockData.Medications;

    public IReadOnlyList<ProviderModel> GetProviders([Service] MockDataService mockData) =>
        mockData.Providers;

    public IReadOnlyList<PayerModel> GetPayers([Service] MockDataService mockData) =>
        mockData.Payers;

    public IReadOnlyList<DiagnosisModel> GetDiagnoses([Service] MockDataService mockData) =>
        mockData.Diagnoses;

    public IReadOnlyList<PARequestModel> GetPARequests([Service] MockDataService mockData) =>
        mockData.GetPARequests();

    public PARequestModel? GetPARequest(string id, [Service] MockDataService mockData) =>
        mockData.GetPARequest(id);

    public PAStatsModel GetPAStats([Service] MockDataService mockData) =>
        mockData.GetPAStats();

    public IReadOnlyList<ActivityItemModel> GetActivity([Service] MockDataService mockData) =>
        mockData.GetActivity();
}
