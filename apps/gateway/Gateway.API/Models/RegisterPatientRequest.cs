// =============================================================================
// <copyright file="RegisterPatientRequest.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Models;

/// <summary>
/// Request body for the patient registration API endpoint.
/// </summary>
public sealed record RegisterPatientRequest
{
    /// <summary>
    /// Gets the FHIR Patient ID to register.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the FHIR Encounter ID to monitor.
    /// </summary>
    public required string EncounterId { get; init; }

    /// <summary>
    /// Gets the athenahealth practice ID.
    /// </summary>
    public required string PracticeId { get; init; }
}
