namespace Gateway.API.Models;

/// <summary>
/// Clinical condition/diagnosis information extracted from FHIR Condition resource.
/// </summary>
public sealed record ConditionInfo
{
    /// <summary>
    /// Gets the FHIR resource ID of the condition.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the diagnosis code (e.g., ICD-10 code).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the code system URI (e.g., ICD-10-CM).
    /// </summary>
    public string? CodeSystem { get; init; }

    /// <summary>
    /// Gets the human-readable display text for the condition.
    /// </summary>
    public string? Display { get; init; }

    /// <summary>
    /// Gets the clinical status (e.g., "active", "resolved").
    /// </summary>
    public string? ClinicalStatus { get; init; }

    /// <summary>
    /// Gets the date when the condition was first recorded or diagnosed.
    /// </summary>
    public DateOnly? OnsetDate { get; init; }
}
