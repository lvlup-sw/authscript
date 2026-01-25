namespace Gateway.API.Models;

/// <summary>
/// Clinical observation/measurement extracted from FHIR Observation resource.
/// </summary>
public sealed record ObservationInfo
{
    /// <summary>
    /// Gets the FHIR resource ID of the observation.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the observation code (e.g., LOINC code).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the code system URI (e.g., LOINC).
    /// </summary>
    public string? CodeSystem { get; init; }

    /// <summary>
    /// Gets the human-readable display text for the observation type.
    /// </summary>
    public string? Display { get; init; }

    /// <summary>
    /// Gets the observation value as a string.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the unit of measurement for the value.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Gets the date/time when the observation was made.
    /// </summary>
    public DateTimeOffset? EffectiveDate { get; init; }
}
