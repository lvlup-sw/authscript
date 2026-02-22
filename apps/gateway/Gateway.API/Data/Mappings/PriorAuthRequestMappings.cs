// =============================================================================
// <copyright file="PriorAuthRequestMappings.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Mappings;

using System.Globalization;
using System.Text.Json;
using Gateway.API.Data.Entities;
using Gateway.API.GraphQL.Models;

/// <summary>
/// Extension methods for mapping between <see cref="PARequestModel"/> and <see cref="PriorAuthRequestEntity"/>.
/// </summary>
public static class PriorAuthRequestMappings
{
    /// <summary>
    /// Shared JSON serializer options for criteria serialization.
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Converts a <see cref="PriorAuthRequestEntity"/> to a <see cref="PARequestModel"/>.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A new <see cref="PARequestModel"/> with values copied from the entity.</returns>
    public static PARequestModel ToModel(this PriorAuthRequestEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var criteria = string.IsNullOrEmpty(entity.CriteriaJson)
            ? []
            : JsonSerializer.Deserialize<List<CriterionModel>>(entity.CriteriaJson, JsonOptions) ?? [];

        return new PARequestModel
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            FhirPatientId = entity.FhirPatientId,
            Patient = new PatientModel
            {
                Id = entity.PatientId,
                Name = entity.PatientName,
                Mrn = entity.PatientMrn,
                Dob = entity.PatientDob ?? string.Empty,
                MemberId = entity.PatientMemberId ?? string.Empty,
                Payer = entity.PatientPayer ?? string.Empty,
                Address = entity.PatientAddress ?? string.Empty,
                Phone = entity.PatientPhone ?? string.Empty,
            },
            ProcedureCode = entity.ProcedureCode,
            ProcedureName = entity.ProcedureName,
            Diagnosis = entity.DiagnosisName ?? string.Empty,
            DiagnosisCode = entity.DiagnosisCode ?? string.Empty,
            Payer = entity.PatientPayer ?? string.Empty,
            ProviderId = entity.ProviderId,
            Provider = entity.ProviderName ?? string.Empty,
            ProviderNpi = entity.ProviderNpi ?? string.Empty,
            ServiceDate = entity.ServiceDate ?? string.Empty,
            PlaceOfService = entity.PlaceOfService ?? string.Empty,
            ClinicalSummary = entity.ClinicalSummary ?? string.Empty,
            Status = entity.Status,
            Confidence = entity.Confidence,
            CreatedAt = entity.CreatedAt.ToString("o"),
            UpdatedAt = entity.UpdatedAt.ToString("o"),
            ReadyAt = entity.ReadyAt?.ToString("o"),
            SubmittedAt = entity.SubmittedAt?.ToString("o"),
            ReviewTimeSeconds = entity.ReviewTimeSeconds,
            Criteria = criteria,
        };
    }

    /// <summary>
    /// Converts a <see cref="PARequestModel"/> to a <see cref="PriorAuthRequestEntity"/>.
    /// </summary>
    /// <param name="model">The model to convert.</param>
    /// <param name="fhirPatientId">Optional FHIR patient ID to set on the entity.</param>
    /// <returns>A new <see cref="PriorAuthRequestEntity"/> with values copied from the model.</returns>
    public static PriorAuthRequestEntity ToEntity(this PARequestModel model, string? fhirPatientId = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        var criteriaJson = model.Criteria.Count > 0
            ? JsonSerializer.Serialize(model.Criteria, JsonOptions)
            : null;

        return new PriorAuthRequestEntity
        {
            Id = model.Id,
            PatientId = model.PatientId,
            FhirPatientId = fhirPatientId ?? model.FhirPatientId ?? model.Patient.Id,
            PatientName = model.Patient.Name,
            PatientMrn = model.Patient.Mrn,
            PatientDob = NullIfEmpty(model.Patient.Dob),
            PatientMemberId = NullIfEmpty(model.Patient.MemberId),
            PatientPayer = NullIfEmpty(model.Patient.Payer),
            PatientAddress = NullIfEmpty(model.Patient.Address),
            PatientPhone = NullIfEmpty(model.Patient.Phone),
            ProcedureCode = model.ProcedureCode,
            ProcedureName = model.ProcedureName,
            DiagnosisCode = NullIfEmpty(model.DiagnosisCode),
            DiagnosisName = NullIfEmpty(model.Diagnosis),
            ProviderId = NullIfEmpty(model.ProviderId),
            ProviderName = NullIfEmpty(model.Provider),
            ProviderNpi = NullIfEmpty(model.ProviderNpi),
            ServiceDate = NullIfEmpty(model.ServiceDate),
            PlaceOfService = NullIfEmpty(model.PlaceOfService),
            ClinicalSummary = NullIfEmpty(model.ClinicalSummary),
            Status = model.Status,
            Confidence = model.Confidence,
            CriteriaJson = criteriaJson,
            CreatedAt = DateTimeOffset.Parse(model.CreatedAt, CultureInfo.InvariantCulture),
            UpdatedAt = DateTimeOffset.Parse(model.UpdatedAt, CultureInfo.InvariantCulture),
            ReadyAt = model.ReadyAt is not null ? DateTimeOffset.Parse(model.ReadyAt, CultureInfo.InvariantCulture) : null,
            SubmittedAt = model.SubmittedAt is not null ? DateTimeOffset.Parse(model.SubmittedAt, CultureInfo.InvariantCulture) : null,
            ReviewTimeSeconds = model.ReviewTimeSeconds,
        };
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrEmpty(value) ? null : value;
}
