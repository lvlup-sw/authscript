namespace Gateway.API.Models;

/// <summary>
/// Request to update a work item's status.
/// </summary>
public sealed record UpdateStatusRequest
{
    /// <summary>
    /// The new status to set.
    /// </summary>
    public required WorkItemStatus Status { get; init; }
}
