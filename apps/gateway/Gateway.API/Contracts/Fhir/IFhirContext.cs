namespace Gateway.API.Contracts.Fhir;

using Hl7.Fhir.Model;

/// <summary>
/// Low-level CRUD interface for FHIR resources.
/// Provides direct access to FHIR server operations.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public interface IFhirContext<TResource> where TResource : Resource
{
    /// <summary>
    /// Reads a single FHIR resource by ID.
    /// </summary>
    /// <param name="id">The resource ID.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resource or an error.</returns>
    Task<Result<TResource>> ReadAsync(string id, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Searches for FHIR resources matching the query.
    /// </summary>
    /// <param name="query">The FHIR search query string.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching resources or an error.</returns>
    Task<Result<IReadOnlyList<TResource>>> SearchAsync(string query, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Creates a new FHIR resource.
    /// </summary>
    /// <param name="resource">The resource to create.</param>
    /// <param name="accessToken">OAuth access token for authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created resource with server-assigned ID, or an error.</returns>
    Task<Result<TResource>> CreateAsync(TResource resource, string accessToken, CancellationToken ct = default);
}
