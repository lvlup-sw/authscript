namespace Gateway.API.Models;

/// <summary>
/// Request for triggering re-hydration of clinical data for a work item.
/// </summary>
public sealed record RehydrateRequest
{
    /// <summary>
    /// Gets the ID of the work item to re-hydrate.
    /// </summary>
    public required string WorkItemId { get; init; }
}
