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
    private readonly ConcurrentDictionary<string, RegisteredPatient> _patients = new();

    /// <inheritdoc/>
    public Task RegisterAsync(RegisteredPatient patient, CancellationToken ct = default)
    {
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
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task UnregisterAsync(string patientId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> UpdateAsync(string patientId, DateTimeOffset lastPolled, string status, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
