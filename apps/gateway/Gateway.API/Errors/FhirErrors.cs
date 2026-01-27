namespace Gateway.API.Errors;

using Gateway.API.Abstractions;

/// <summary>
/// Domain-specific errors for FHIR operations.
/// </summary>
public static class FhirErrors
{
    /// <summary>
    /// FHIR service is unavailable.
    /// </summary>
    public static readonly Error ServiceUnavailable =
        new("Fhir.ServiceUnavailable", "FHIR service is unavailable", ErrorType.Infrastructure);

    /// <summary>
    /// FHIR request timed out.
    /// </summary>
    public static readonly Error Timeout =
        new("Fhir.Timeout", "FHIR request timed out", ErrorType.Infrastructure);

    /// <summary>
    /// Failed to authenticate with FHIR server.
    /// </summary>
    public static readonly Error AuthenticationFailed =
        new("Fhir.AuthFailed", "Failed to authenticate with FHIR server", ErrorType.Unauthorized);

    /// <summary>
    /// Creates a NotFound error for a FHIR resource.
    /// </summary>
    /// <param name="resourceType">The FHIR resource type (e.g., "Patient").</param>
    /// <param name="id">The resource identifier.</param>
    /// <returns>A NotFound error.</returns>
    public static Error NotFound(string resourceType, string id) =>
        ErrorFactory.NotFound(resourceType, id);

    /// <summary>
    /// Creates an error for invalid FHIR response.
    /// </summary>
    /// <param name="details">Details about why the response is invalid.</param>
    /// <returns>An InvalidResponse error.</returns>
    public static Error InvalidResponse(string details) =>
        new("Fhir.InvalidResponse", $"Invalid FHIR response: {details}", ErrorType.Infrastructure);

    /// <summary>
    /// Creates an error for network issues when communicating with FHIR server.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception, if any.</param>
    /// <returns>A NetworkError.</returns>
    public static Error NetworkError(string message, Exception? inner = null) =>
        new("Fhir.NetworkError", message, ErrorType.Infrastructure) { Inner = inner };
}
