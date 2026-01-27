namespace Gateway.API.Contracts;

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly record struct Result<T>
{
    /// <summary>
    /// Gets the success value, if the result is successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error, if the result is a failure.
    /// </summary>
    public FhirError? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        Error = null;
    }

    private Result(FhirError error)
    {
        Value = default;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(FhirError error) => new(error);

    /// <summary>
    /// Matches on the result, executing the appropriate function based on success or failure.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSuccess">Function to execute on success.</param>
    /// <param name="onFailure">Function to execute on failure.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<FhirError, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

/// <summary>
/// Represents an error from a FHIR operation.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="Inner">The inner exception, if any.</param>
public record FhirError(string Code, string Message, Exception? Inner = null)
{
    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="resourceType">The FHIR resource type.</param>
    /// <param name="id">The resource ID.</param>
    /// <returns>A not found error.</returns>
    public static FhirError NotFound(string resourceType, string id)
        => new("NOT_FOUND", $"{resourceType}/{id} not found");

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An unauthorized error.</returns>
    public static FhirError Unauthorized(string message = "Access token is invalid or expired")
        => new("UNAUTHORIZED", message);

    /// <summary>
    /// Creates a network error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception.</param>
    /// <returns>A network error.</returns>
    public static FhirError Network(string message, Exception? inner = null)
        => new("NETWORK_ERROR", message, inner);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <returns>A validation error.</returns>
    public static FhirError Validation(string message)
        => new("VALIDATION_ERROR", message);

    /// <summary>
    /// Creates an invalid response error.
    /// </summary>
    /// <param name="message">The error message describing the invalid response.</param>
    /// <returns>An invalid response error.</returns>
    public static FhirError InvalidResponse(string message)
        => new("INVALID_RESPONSE", message);
}
