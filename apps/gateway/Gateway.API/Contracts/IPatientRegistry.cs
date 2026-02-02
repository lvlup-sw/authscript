// =============================================================================
// <copyright file="IPatientRegistry.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Registry for managing patients awaiting encounter completion.
/// Tracks registered patients whose encounters are being polled for status changes.
/// </summary>
public interface IPatientRegistry
{
    /// <summary>
    /// Registers a patient for encounter monitoring.
    /// </summary>
    /// <param name="patient">The patient registration details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterAsync(RegisteredPatient patient, CancellationToken ct = default);

    /// <summary>
    /// Gets all active registered patients awaiting encounter completion.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of active registered patients.</returns>
    Task<IReadOnlyList<RegisteredPatient>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Unregisters a patient from encounter monitoring.
    /// </summary>
    /// <param name="patientId">The FHIR Patient ID to unregister.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnregisterAsync(string patientId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific registered patient by their FHIR Patient ID.
    /// </summary>
    /// <param name="patientId">The FHIR Patient ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registered patient if found; otherwise, null.</returns>
    Task<RegisteredPatient?> GetAsync(string patientId, CancellationToken ct = default);

    /// <summary>
    /// Updates the polling status for a registered patient.
    /// </summary>
    /// <param name="patientId">The FHIR Patient ID to update.</param>
    /// <param name="lastPolled">The timestamp of the last poll.</param>
    /// <param name="status">The current encounter status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the patient was found and updated; otherwise, false.</returns>
    Task<bool> UpdateAsync(string patientId, DateTimeOffset lastPolled, string status, CancellationToken ct = default);
}
