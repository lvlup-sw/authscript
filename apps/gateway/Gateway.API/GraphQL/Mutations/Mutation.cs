using Gateway.API.GraphQL.Inputs;
using Gateway.API.GraphQL.Models;
using Gateway.API.Services;

namespace Gateway.API.GraphQL.Mutations;

public sealed class Mutation
{
    public PARequestModel CreatePARequest(CreatePARequestInput input, [Service] MockDataService mockData)
    {
        var patient = new PatientModel
        {
            Id = input.Patient.Id,
            Name = input.Patient.Name,
            Mrn = input.Patient.Mrn,
            Dob = input.Patient.Dob,
            MemberId = input.Patient.MemberId,
            Payer = input.Patient.Payer,
            Address = input.Patient.Address,
            Phone = input.Patient.Phone,
        };
        return mockData.CreatePARequest(
            patient,
            input.ProcedureCode,
            input.DiagnosisCode,
            input.DiagnosisName,
            input.ProviderId ?? "DR001");
    }

    public PARequestModel? UpdatePARequest(UpdatePARequestInput input, [Service] MockDataService mockData)
    {
        var criteria = input.Criteria?.Select(c => new CriterionModel { Met = c.Met, Label = c.Label, Reason = c.Reason }).ToList();
        return mockData.UpdatePARequest(
            input.Id,
            input.Diagnosis,
            input.DiagnosisCode,
            input.ServiceDate,
            input.PlaceOfService,
            input.ClinicalSummary,
            criteria);
    }

    public async Task<PARequestModel?> ProcessPARequest(string id, [Service] MockDataService mockData, CancellationToken cancellationToken)
    {
        return await mockData.ProcessPARequestAsync(id, cancellationToken);
    }

    public PARequestModel? SubmitPARequest(string id, [Service] MockDataService mockData, int addReviewTimeSeconds = 0)
    {
        return mockData.SubmitPARequest(id, addReviewTimeSeconds);
    }

    public PARequestModel? AddReviewTime(string id, int seconds, [Service] MockDataService mockData)
    {
        return mockData.AddReviewTime(id, seconds);
    }

    public bool DeletePARequest(string id, [Service] MockDataService mockData)
    {
        return mockData.DeletePARequest(id);
    }
}
