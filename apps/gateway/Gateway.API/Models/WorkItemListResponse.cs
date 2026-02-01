namespace Gateway.API.Models;

/// <summary>
/// Response containing a list of work items.
/// </summary>
public sealed record WorkItemListResponse
{
    /// <summary>
    /// The list of work items.
    /// </summary>
    public required List<WorkItem> Items { get; init; }

    /// <summary>
    /// Total count of items (for pagination).
    /// </summary>
    public required int Total { get; init; }
}
