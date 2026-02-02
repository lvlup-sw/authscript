using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Repository interface for persisting and retrieving work items.
/// </summary>
public interface IWorkItemStore
{
    /// <summary>
    /// Creates a new work item.
    /// </summary>
    /// <param name="workItem">The work item to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created work item.</returns>
    Task<string> CreateAsync(WorkItem workItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a work item by its ID.
    /// </summary>
    /// <param name="id">The work item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The work item if found, null otherwise.</returns>
    Task<WorkItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a work item.
    /// </summary>
    /// <param name="id">The work item ID.</param>
    /// <param name="newStatus">The new status to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update succeeded, false if work item not found.</returns>
    Task<bool> UpdateStatusAsync(string id, WorkItemStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all work items for a given encounter.
    /// </summary>
    /// <param name="encounterId">The FHIR Encounter ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of work items associated with the encounter.</returns>
    Task<List<WorkItem>> GetByEncounterAsync(string encounterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all work items, optionally filtered.
    /// </summary>
    /// <param name="encounterId">Optional encounter ID filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching work items.</returns>
    Task<List<WorkItem>> GetAllAsync(
        string? encounterId = null,
        WorkItemStatus? status = null,
        CancellationToken cancellationToken = default);
}
