namespace Gateway.API.Endpoints;

/// <summary>
/// Request body for the submit endpoint.
/// </summary>
public sealed record SubmitRequest
{
    /// <summary>
    /// Gets the FHIR Patient resource ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the optional FHIR Encounter resource ID for context.
    /// </summary>
    public string? EncounterId { get; init; }
}
