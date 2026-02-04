// =============================================================================
// <copyright file="PostgresPatientRegistry.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Services;

using Gateway.API.Contracts;
using Gateway.API.Data;
using Gateway.API.Data.Mappings;
using Gateway.API.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IPatientRegistry"/> using EF Core.
/// </summary>
public sealed class PostgresPatientRegistry : IPatientRegistry
{
    private readonly GatewayDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresPatientRegistry"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PostgresPatientRegistry(GatewayDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task RegisterAsync(RegisteredPatient patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);

        var entity = patient.ToEntity();
        _context.RegisteredPatients.Add(entity);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<RegisteredPatient?> GetAsync(string patientId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);

        var entity = await _context.RegisteredPatients
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PatientId == patientId, ct)
            .ConfigureAwait(false);

        return entity?.ToModel();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RegisteredPatient>> GetActiveAsync(CancellationToken ct = default)
    {
        var entities = await _context.RegisteredPatients
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(e => e.ToModel()).ToList();
    }

    /// <inheritdoc/>
    public async Task UnregisterAsync(string patientId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);

        var entity = await _context.RegisteredPatients
            .FirstOrDefaultAsync(e => e.PatientId == patientId, ct)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            _context.RegisteredPatients.Remove(entity);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(string patientId, DateTimeOffset lastPolled, string status, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);

        var entity = await _context.RegisteredPatients
            .FirstOrDefaultAsync(e => e.PatientId == patientId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        entity.LastPolledAt = lastPolled;
        entity.CurrentEncounterStatus = status;
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
