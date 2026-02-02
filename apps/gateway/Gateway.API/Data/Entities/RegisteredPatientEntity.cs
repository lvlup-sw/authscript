// =============================================================================
// <copyright file="RegisteredPatientEntity.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Entities;

/// <summary>
/// Entity Framework Core entity representing a registered patient for encounter monitoring.
/// Maps to the registered_patients table in the database.
/// </summary>
public sealed class RegisteredPatientEntity
{
    /// <summary>
    /// Gets or sets the FHIR Patient ID (primary key).
    /// </summary>
    public required string PatientId { get; set; }

    /// <summary>
    /// Gets or sets the FHIR Encounter ID being monitored.
    /// </summary>
    public required string EncounterId { get; set; }

    /// <summary>
    /// Gets or sets the athenahealth practice ID.
    /// </summary>
    public required string PracticeId { get; set; }

    /// <summary>
    /// Gets or sets the work item ID created on registration.
    /// </summary>
    public required string WorkItemId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the patient was registered for monitoring.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the encounter was last polled for status updates.
    /// </summary>
    public DateTimeOffset? LastPolledAt { get; set; }

    /// <summary>
    /// Gets or sets the current encounter status from the last poll.
    /// </summary>
    public string? CurrentEncounterStatus { get; set; }
}
