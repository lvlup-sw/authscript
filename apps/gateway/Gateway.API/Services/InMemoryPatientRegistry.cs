// =============================================================================
// <copyright file="InMemoryPatientRegistry.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Collections.Concurrent;
using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// In-memory implementation of <see cref="IPatientRegistry"/> for development and testing.
/// Uses a thread-safe concurrent dictionary for storage.
/// </summary>
public sealed class InMemoryPatientRegistry : IPatientRegistry
{
    private static readonly TimeSpan ExpirationTime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, RegisteredPatient> _patients = new();

    /// <inheritdoc/>
    public Task RegisterAsync(RegisteredPatient patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (string.IsNullOrWhiteSpace(patient.PatientId))
        {
            throw new ArgumentException("PatientId is required.", nameof(patient));
        }

        _patients[patient.PatientId] = patient;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<RegisteredPatient?> GetAsync(string patientId, CancellationToken ct = default)
    {
        _patients.TryGetValue(patientId, out var patient);
        return Task.FromResult(patient);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<RegisteredPatient>> GetActiveAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - ExpirationTime;
        var active = _patients.Values
            .Where(p => p.RegisteredAt > cutoff)
            .ToList();
        return Task.FromResult<IReadOnlyList<RegisteredPatient>>(active);
    }

    /// <inheritdoc/>
    public Task UnregisterAsync(string patientId, CancellationToken ct = default)
    {
        _patients.TryRemove(patientId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> UpdateAsync(string patientId, DateTimeOffset lastPolled, string status, CancellationToken ct = default)
    {
        if (!_patients.TryGetValue(patientId, out var existing))
        {
            return Task.FromResult(false);
        }

        var updated = existing with
        {
            LastPolledAt = lastPolled,
            CurrentEncounterStatus = status
        };

        var success = _patients.TryUpdate(patientId, updated, existing);
        return Task.FromResult(success);
    }
}
