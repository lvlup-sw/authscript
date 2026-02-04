namespace Gateway.API.Models;

/// <summary>
/// FHIR ServiceRequest resource information representing orders/referrals.
/// Used to identify treatments requiring prior authorization.
/// </summary>
public sealed record ServiceRequestInfo
{
    /// <summary>
    /// Gets the FHIR resource ID of the service request.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the status of the service request (e.g., "active", "completed", "cancelled").
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the code representing the ordered procedure (e.g., CPT code).
    /// </summary>
    public required CodeableConcept Code { get; init; }

    /// <summary>
    /// Gets the optional link to the associated encounter.
    /// </summary>
    public string? EncounterId { get; init; }

    /// <summary>
    /// Gets the date and time when the order was created.
    /// </summary>
    public DateTimeOffset? AuthoredOn { get; init; }
}
