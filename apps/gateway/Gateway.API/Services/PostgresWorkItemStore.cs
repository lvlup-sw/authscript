// =============================================================================
// <copyright file="PostgresWorkItemStore.cs" company="Levelup Software">
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
/// PostgreSQL-backed implementation of IWorkItemStore using EF Core.
/// </summary>
public sealed class PostgresWorkItemStore : IWorkItemStore
{
    private readonly GatewayDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresWorkItemStore"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PostgresWorkItemStore(GatewayDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<string> CreateAsync(WorkItem workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var entity = workItem.ToEntity();
        _context.WorkItems.Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return workItem.Id;
    }

    /// <inheritdoc/>
    public async Task<WorkItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.WorkItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToModel();
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateStatusAsync(string id, WorkItemStatus newStatus, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.WorkItems
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        entity.Status = newStatus;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(string id, WorkItem updated, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(updated);

        var entity = await _context.WorkItems
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        entity.PatientId = updated.PatientId;
        entity.EncounterId = updated.EncounterId;
        entity.ServiceRequestId = updated.ServiceRequestId;
        entity.ProcedureCode = updated.ProcedureCode;
        entity.Status = updated.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<WorkItem>> GetByEncounterAsync(string encounterId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encounterId);

        var entities = await _context.WorkItems
            .AsNoTracking()
            .Where(e => e.EncounterId == encounterId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(e => e.ToModel()).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<WorkItem>> GetAllAsync(
        string? encounterId = null,
        WorkItemStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            query = query.Where(e => e.EncounterId == encounterId);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return entities.Select(e => e.ToModel()).ToList();
    }
}
