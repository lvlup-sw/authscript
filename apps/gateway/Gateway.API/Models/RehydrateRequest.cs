namespace Gateway.API.Models;

/// <summary>
/// Request for triggering re-hydration of clinical data for a work item.
/// Work item ID comes from the route parameter.
/// </summary>
public sealed record RehydrateRequest
{
    /// <summary>
    /// Optional OAuth access token. If not provided, uses token resolver.
    /// For preview/testing when token resolver is not configured.
    /// </summary>
    public string? AccessToken { get; init; }
}
