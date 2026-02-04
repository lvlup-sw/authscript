namespace Gateway.API.Models;

/// <summary>
/// Response from the work item rehydrate endpoint.
/// </summary>
public sealed record RehydrateResponse
{
    /// <summary>
    /// Gets the ID of the rehydrated work item.
    /// </summary>
    public required string WorkItemId { get; init; }

    /// <summary>
    /// Gets the new status of the work item after reanalysis.
    /// </summary>
    public required string NewStatus { get; init; }

    /// <summary>
    /// Gets an optional message describing the result.
    /// </summary>
    public string? Message { get; init; }
}
