// =============================================================================
// <copyright file="Mutation.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.GraphQL.Inputs;
using Gateway.API.GraphQL.Models;
using Gateway.API.Services;

namespace Gateway.API.GraphQL.Mutations;

/// <summary>
/// GraphQL mutation resolvers for PA request operations.
/// </summary>
public sealed class Mutation
{
    /// <summary>Creates a new PA request.</summary>
    public PARequestModel CreatePARequest(CreatePARequestInput input, [Service] IDataService dataService)
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
        return dataService.CreatePARequest(
            patient,
            input.ProcedureCode,
            input.DiagnosisCode,
            input.DiagnosisName,
            input.ProviderId ?? "DR001");
    }

    /// <summary>Updates an existing PA request.</summary>
    public PARequestModel? UpdatePARequest(UpdatePARequestInput input, [Service] IDataService dataService)
    {
        var criteria = input.Criteria?.Select(c => new CriterionModel { Met = c.Met, Label = c.Label, Reason = c.Reason }).ToList();
        return dataService.UpdatePARequest(
            input.Id,
            input.Diagnosis,
            input.DiagnosisCode,
            input.ServiceDate,
            input.PlaceOfService,
            input.ClinicalSummary,
            criteria);
    }

    /// <summary>Processes a PA request through AI analysis.</summary>
    public async Task<PARequestModel?> ProcessPARequest(string id, [Service] IDataService dataService, CancellationToken cancellationToken)
    {
        return await dataService.ProcessPARequestAsync(id, cancellationToken);
    }

    /// <summary>Submits a PA request for insurance review.</summary>
    public PARequestModel? SubmitPARequest(string id, [Service] IDataService dataService, int addReviewTimeSeconds = 0)
    {
        return dataService.SubmitPARequest(id, addReviewTimeSeconds);
    }

    /// <summary>Adds review time to a PA request.</summary>
    public PARequestModel? AddReviewTime(string id, int seconds, [Service] IDataService dataService)
    {
        return dataService.AddReviewTime(id, seconds);
    }

    /// <summary>Deletes a PA request.</summary>
    public bool DeletePARequest(string id, [Service] IDataService dataService)
    {
        return dataService.DeletePARequest(id);
    }

    /// <summary>Approves a PA request that is waiting for insurance.</summary>
    public PARequestModel? ApprovePARequest(string id, [Service] IDataService dataService)
        => dataService.ApprovePA(id);

    /// <summary>Denies a PA request that is waiting for insurance.</summary>
    public PARequestModel? DenyPARequest(string id, string reason, [Service] IDataService dataService)
        => dataService.DenyPA(id, reason);
}
