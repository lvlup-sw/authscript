namespace Gateway.API.Models;

/// <summary>
/// Medical procedure information extracted from FHIR Procedure resource.
/// </summary>
public sealed record ProcedureInfo
{
    /// <summary>
    /// Gets the FHIR resource ID of the procedure.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the procedure code (e.g., CPT code).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the code system URI (e.g., CPT).
    /// </summary>
    public string? CodeSystem { get; init; }

    /// <summary>
    /// Gets the human-readable display text for the procedure.
    /// </summary>
    public string? Display { get; init; }

    /// <summary>
    /// Gets the procedure status (e.g., "completed", "in-progress").
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the date when the procedure was performed.
    /// </summary>
    public DateOnly? PerformedDate { get; init; }
}
