using System.Text.Json;

namespace Gateway.API.Contracts;

/// <summary>
/// Low-level HTTP interface for FHIR server operations.
/// Handles authentication and HTTP transport, returning raw JSON responses.
/// </summary>
public interface IFhirHttpClient
{
    /// <summary>
    /// Reads a single FHIR resource by ID.
    /// </summary>
    /// <param name="resourceType">The FHIR resource type (e.g., "Patient", "Condition").</param>
    /// <param name="id">The resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw JSON resource or an error.</returns>
    Task<Result<JsonElement>> ReadAsync(
        string resourceType,
        string id,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for FHIR resources matching the query.
    /// </summary>
    /// <param name="resourceType">The FHIR resource type.</param>
    /// <param name="query">The FHIR search query string.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw JSON bundle or an error.</returns>
    Task<Result<JsonElement>> SearchAsync(
        string resourceType,
        string query,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new FHIR resource.
    /// </summary>
    /// <param name="resourceType">The FHIR resource type.</param>
    /// <param name="resourceJson">The resource JSON to create.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created resource JSON with server-assigned ID, or an error.</returns>
    Task<Result<JsonElement>> CreateAsync(
        string resourceType,
        string resourceJson,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Reads binary content by ID.
    /// </summary>
    /// <param name="id">The Binary resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The binary content or an error.</returns>
    Task<Result<byte[]>> ReadBinaryAsync(
        string id,
        string accessToken,
        CancellationToken ct = default);
}
