// =============================================================================
// <copyright file="EncounterCompletedEvent.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Models;

/// <summary>
/// Event emitted when a monitored encounter transitions to "finished" status.
/// Published to a channel and consumed by the EncounterProcessor.
/// </summary>
public sealed record EncounterCompletedEvent
{
    /// <summary>
    /// Gets the FHIR Patient ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the FHIR Encounter ID that completed.
    /// </summary>
    public required string EncounterId { get; init; }

    /// <summary>
    /// Gets the athenahealth practice ID.
    /// </summary>
    public required string PracticeId { get; init; }

    /// <summary>
    /// Gets the associated work item ID.
    /// </summary>
    public required string WorkItemId { get; init; }
}
