using System.Collections.Concurrent;
using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// In-memory implementation of work item storage for MVP.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public sealed class InMemoryWorkItemStore : IWorkItemStore
{
    private readonly ConcurrentDictionary<string, WorkItem> _store = new();

    /// <inheritdoc />
    public Task<string> CreateAsync(WorkItem workItem, CancellationToken cancellationToken = default)
    {
        _store[workItem.Id] = workItem;
        return Task.FromResult(workItem.Id);
    }

    /// <inheritdoc />
    public Task<WorkItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var workItem);
        return Task.FromResult(workItem);
    }

    /// <inheritdoc />
    public Task<bool> UpdateStatusAsync(string id, WorkItemStatus newStatus, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 10;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            if (!_store.TryGetValue(id, out var existing))
            {
                return Task.FromResult(false);
            }

            var updated = existing with
            {
                Status = newStatus,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // Atomically update only if the value hasn't changed since we read it
            if (_store.TryUpdate(id, updated, existing))
            {
                return Task.FromResult(true);
            }
            // If TryUpdate failed, another thread modified the entry - retry
        }

        // Exhausted retries due to concurrent modifications
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(string id, WorkItem updated, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 10;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            if (!_store.TryGetValue(id, out var existing))
            {
                return Task.FromResult(false);
            }

            // Atomically update only if the value hasn't changed since we read it
            if (_store.TryUpdate(id, updated, existing))
            {
                return Task.FromResult(true);
            }
            // If TryUpdate failed, another thread modified the entry - retry
        }

        // Exhausted retries due to concurrent modifications
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<List<WorkItem>> GetByEncounterAsync(string encounterId, CancellationToken cancellationToken = default)
    {
        var matches = _store.Values
            .Where(w => w.EncounterId == encounterId)
            .ToList();

        return Task.FromResult(matches);
    }

    /// <inheritdoc />
    public Task<List<WorkItem>> GetAllAsync(
        string? encounterId = null,
        WorkItemStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _store.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(encounterId))
        {
            query = query.Where(w => w.EncounterId == encounterId);
        }

        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        return Task.FromResult(query.ToList());
    }
}
