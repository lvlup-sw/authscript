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
        if (!_store.TryGetValue(id, out var existing))
        {
            return Task.FromResult(false);
        }

        var updated = existing with
        {
            Status = newStatus,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _store[id] = updated;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<List<WorkItem>> GetByEncounterAsync(string encounterId, CancellationToken cancellationToken = default)
    {
        var matches = _store.Values
            .Where(w => w.EncounterId == encounterId)
            .ToList();

        return Task.FromResult(matches);
    }
}
