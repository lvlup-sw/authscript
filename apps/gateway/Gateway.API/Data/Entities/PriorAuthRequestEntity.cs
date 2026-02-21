// =============================================================================
// <copyright file="PriorAuthRequestEntity.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Entities;

/// <summary>
/// Entity Framework Core entity representing a prior authorization request in the database.
/// </summary>
public sealed class PriorAuthRequestEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the PA request.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the numeric Athena patient ID (e.g. "60178").
    /// </summary>
    public required string PatientId { get; set; }

    /// <summary>
    /// Gets or sets the FHIR logical patient ID (e.g. "a-195900.E-60178").
    /// </summary>
    public required string FhirPatientId { get; set; }

    /// <summary>
    /// Gets or sets the patient's full name.
    /// </summary>
    public required string PatientName { get; set; }

    /// <summary>
    /// Gets or sets the patient's medical record number.
    /// </summary>
    public required string PatientMrn { get; set; }

    /// <summary>
    /// Gets or sets the patient's date of birth.
    /// </summary>
    public string? PatientDob { get; set; }

    /// <summary>
    /// Gets or sets the patient's insurance member ID.
    /// </summary>
    public string? PatientMemberId { get; set; }

    /// <summary>
    /// Gets or sets the patient's insurance payer.
    /// </summary>
    public string? PatientPayer { get; set; }

    /// <summary>
    /// Gets or sets the patient's address.
    /// </summary>
    public string? PatientAddress { get; set; }

    /// <summary>
    /// Gets or sets the patient's phone number.
    /// </summary>
    public string? PatientPhone { get; set; }

    /// <summary>
    /// Gets or sets the CPT procedure code.
    /// </summary>
    public required string ProcedureCode { get; set; }

    /// <summary>
    /// Gets or sets the procedure name.
    /// </summary>
    public required string ProcedureName { get; set; }

    /// <summary>
    /// Gets or sets the ICD-10 diagnosis code.
    /// </summary>
    public string? DiagnosisCode { get; set; }

    /// <summary>
    /// Gets or sets the diagnosis name.
    /// </summary>
    public string? DiagnosisName { get; set; }

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Gets or sets the provider NPI number.
    /// </summary>
    public string? ProviderNpi { get; set; }

    /// <summary>
    /// Gets or sets the date of service.
    /// </summary>
    public string? ServiceDate { get; set; }

    /// <summary>
    /// Gets or sets the place of service.
    /// </summary>
    public string? PlaceOfService { get; set; }

    /// <summary>
    /// Gets or sets the clinical summary text.
    /// </summary>
    public string? ClinicalSummary { get; set; }

    /// <summary>
    /// Gets or sets the current status (draft, ready, waiting_for_insurance, approved, denied).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the AI confidence score.
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// Gets or sets the criteria as a JSON string (stored as jsonb in PostgreSQL).
    /// </summary>
    public string? CriteriaJson { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request became ready for review.
    /// </summary>
    public DateTimeOffset? ReadyAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request was submitted.
    /// </summary>
    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>
    /// Gets or sets the total seconds the user spent on the review page.
    /// </summary>
    public int ReviewTimeSeconds { get; set; }
}
