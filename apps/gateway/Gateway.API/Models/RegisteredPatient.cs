// =============================================================================
// <copyright file="RegisteredPatient.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Models;

/// <summary>
/// Represents a patient registered for encounter monitoring.
/// Tracks patients whose encounters are being polled for status changes.
/// </summary>
public sealed record RegisteredPatient
{
    /// <summary>
    /// Gets the FHIR Patient ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the FHIR Encounter ID being monitored.
    /// </summary>
    public required string EncounterId { get; init; }

    /// <summary>
    /// Gets the athenahealth practice ID.
    /// </summary>
    public required string PracticeId { get; init; }

    /// <summary>
    /// Gets the work item ID created on registration.
    /// Links this registered patient to the work item tracking the prior authorization workflow.
    /// </summary>
    public required string WorkItemId { get; init; }

    /// <summary>
    /// Gets the date and time when the patient was registered for monitoring.
    /// </summary>
    public required DateTimeOffset RegisteredAt { get; init; }

    /// <summary>
    /// Gets the date and time when the encounter was last polled for status updates.
    /// </summary>
    public DateTimeOffset? LastPolledAt { get; init; }

    /// <summary>
    /// Gets the current encounter status from the last poll.
    /// </summary>
    public string? CurrentEncounterStatus { get; init; }
}
