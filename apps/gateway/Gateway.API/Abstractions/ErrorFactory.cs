namespace Gateway.API.Abstractions;

/// <summary>
/// Factory methods for creating common error types.
/// </summary>
public static class ErrorFactory
{
    /// <summary>
    /// Creates a NotFound error for a specific resource.
    /// </summary>
    /// <param name="resource">The resource type (e.g., "Patient").</param>
    /// <param name="id">The resource identifier.</param>
    /// <returns>A NotFound error.</returns>
    public static Error NotFound(string resource, string id)
        => new($"{resource}.NotFound", $"{resource}/{id} not found", ErrorType.NotFound);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <returns>A Validation error.</returns>
    public static Error Validation(string message)
        => new("Validation.Failed", message, ErrorType.Validation);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An Unauthorized error.</returns>
    public static Error Unauthorized(string message = "Authentication required")
        => new("Auth.Unauthorized", message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates an infrastructure error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception, if any.</param>
    /// <returns>An Infrastructure error.</returns>
    public static Error Infrastructure(string message, Exception? inner = null)
        => new("Infrastructure.Error", message, ErrorType.Infrastructure) { Inner = inner };

    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception, if any.</param>
    /// <returns>An Unexpected error.</returns>
    public static Error Unexpected(string message, Exception? inner = null)
        => new("Unexpected.Error", message, ErrorType.Unexpected) { Inner = inner };
}
