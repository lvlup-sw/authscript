namespace Gateway.API.Models;

/// <summary>
/// Response for the GetAnalysis endpoint.
/// </summary>
public sealed record AnalysisResponse
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public required string TransactionId { get; init; }

    /// <summary>
    /// Gets the analysis status: "pending", "in_progress", or "completed".
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the PA form data when analysis is complete.
    /// </summary>
    public PAFormData? FormData { get; init; }

    /// <summary>
    /// Gets the message describing the current state.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the timestamp of when the status was last updated.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Response for the GetStatus endpoint.
/// </summary>
public sealed record StatusResponse
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public required string TransactionId { get; init; }

    /// <summary>
    /// Gets the current processing step.
    /// </summary>
    public required string Step { get; init; }

    /// <summary>
    /// Gets a human-readable message about the current state.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public required int Progress { get; init; }

    /// <summary>
    /// Gets the timestamp of when the status was last updated.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Response for the SubmitToEpic endpoint.
/// </summary>
public sealed record SubmitResponse
{
    /// <summary>
    /// Gets the transaction identifier.
    /// </summary>
    public required string TransactionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the submission succeeded.
    /// </summary>
    public required bool Submitted { get; init; }

    /// <summary>
    /// Gets the Epic DocumentReference ID when submitted successfully.
    /// </summary>
    public string? DocumentId { get; init; }

    /// <summary>
    /// Gets a message describing the result.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the timestamp of the submission.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Generic error response.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling.
    /// </summary>
    public string? Code { get; init; }
}

/// <summary>
/// Request body for the SubmitToEpic endpoint.
/// </summary>
public sealed record SubmitToEpicRequest
{
    /// <summary>
    /// Gets the FHIR Patient resource ID.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// Gets the optional FHIR Encounter resource ID for context.
    /// </summary>
    public string? EncounterId { get; init; }

    /// <summary>
    /// Gets the OAuth access token for Epic authentication.
    /// </summary>
    public required string AccessToken { get; init; }
}
