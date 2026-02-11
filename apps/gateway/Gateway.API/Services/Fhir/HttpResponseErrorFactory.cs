using System.Net;
using Gateway.API.Contracts;

namespace Gateway.API.Services.Fhir;

/// <summary>
/// Factory for creating Result failures from HTTP response status codes.
/// Consolidates HTTP error handling logic for FHIR operations.
/// </summary>
public static class HttpResponseErrorFactory
{
    /// <summary>
    /// Validates an HTTP response for read operations and returns an error if the response indicates failure.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="response">The HTTP response to validate.</param>
    /// <param name="resourceType">The FHIR resource type being read.</param>
    /// <param name="id">The resource identifier.</param>
    /// <returns>A failure Result if the response indicates an error; null if the response is successful.</returns>
    public static Gateway.API.Contracts.Result<T>? ValidateReadResponse<T>(
        HttpResponseMessage response,
        string resourceType,
        string id)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Gateway.API.Contracts.Result<T>.Failure(FhirError.NotFound(resourceType, id));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Gateway.API.Contracts.Result<T>.Failure(FhirError.Unauthorized());
        }

        return null;
    }

    /// <summary>
    /// Validates an HTTP response for search operations and returns an error if the response indicates failure.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="response">The HTTP response to validate.</param>
    /// <param name="resourceType">The FHIR resource type being searched.</param>
    /// <returns>A failure Result if the response indicates an error; null if the response is successful.</returns>
    public static Gateway.API.Contracts.Result<T>? ValidateSearchResponse<T>(
        HttpResponseMessage response,
        string resourceType)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Gateway.API.Contracts.Result<T>.Failure(
                FhirError.InvalidResponse($"FHIR {resourceType} search endpoint not found"));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Gateway.API.Contracts.Result<T>.Failure(FhirError.Unauthorized());
        }

        return null;
    }

    /// <summary>
    /// Validates an HTTP response for create operations and returns an error if the response indicates failure.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="response">The HTTP response to validate.</param>
    /// <param name="validationErrorContent">The error content for validation failures (UnprocessableEntity).</param>
    /// <returns>A failure Result if the response indicates an error; null if the response is successful.</returns>
    public static Gateway.API.Contracts.Result<T>? ValidateCreateResponse<T>(
        HttpResponseMessage response,
        string? validationErrorContent = null)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Gateway.API.Contracts.Result<T>.Failure(FhirError.Unauthorized());
        }

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            return Gateway.API.Contracts.Result<T>.Failure(FhirError.Validation(validationErrorContent ?? "Validation failed"));
        }

        return null;
    }

    /// <summary>
    /// Creates a network error Result from an HttpRequestException.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="exception">The HTTP request exception.</param>
    /// <returns>A failure Result with network error details.</returns>
    public static Gateway.API.Contracts.Result<T> NetworkError<T>(HttpRequestException exception)
    {
        return Gateway.API.Contracts.Result<T>.Failure(FhirError.Network(exception.Message, exception));
    }

    /// <summary>
    /// Creates a validation error Result from a JsonException.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="exception">The JSON exception.</param>
    /// <returns>A failure Result with validation error details.</returns>
    public static Gateway.API.Contracts.Result<T> JsonError<T>(System.Text.Json.JsonException exception)
    {
        return Gateway.API.Contracts.Result<T>.Failure(FhirError.Validation($"Invalid JSON response: {exception.Message}"));
    }

    /// <summary>
    /// Creates a validation error Result for deserialization failures.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="resourceType">The FHIR resource type.</param>
    /// <param name="id">The resource identifier (optional).</param>
    /// <returns>A failure Result with deserialization error details.</returns>
    public static Gateway.API.Contracts.Result<T> DeserializationError<T>(string resourceType, string? id = null)
    {
        var resource = id is not null ? $"{resourceType}/{id}" : resourceType;
        return Gateway.API.Contracts.Result<T>.Failure(FhirError.Validation($"Failed to deserialize {resource}"));
    }
}
