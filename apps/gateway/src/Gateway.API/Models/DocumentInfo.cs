namespace Gateway.API.Models;

/// <summary>
/// Clinical document information extracted from FHIR DocumentReference resource.
/// </summary>
public sealed record DocumentInfo
{
    /// <summary>
    /// Gets the FHIR resource ID of the document.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the document type code.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the MIME content type of the document (e.g., "application/pdf").
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the raw binary data of the document.
    /// </summary>
    public byte[]? Data { get; init; }

    /// <summary>
    /// Gets the human-readable title of the document.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the date when the document was created or indexed.
    /// </summary>
    public DateTimeOffset? Date { get; init; }
}
