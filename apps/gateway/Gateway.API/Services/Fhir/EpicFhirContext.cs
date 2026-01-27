using System.Net;
using System.Net.Http.Headers;
using Gateway.API.Abstractions;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Errors;
using Hl7.Fhir.Model;

namespace Gateway.API.Services.Fhir;

/// <summary>
/// Generic FHIR context implementation for Epic's FHIR R4 API.
/// Provides low-level CRUD operations with Result-based error handling.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public class EpicFhirContext<TResource> : IFhirContext<TResource> where TResource : Resource
{
    private readonly HttpClient _httpClient;
    private readonly IFhirSerializer _fhirSerializer;
    private readonly ILogger<EpicFhirContext<TResource>> _logger;
    private readonly string _resourceType;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicFhirContext{TResource}"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with Epic FHIR base URL.</param>
    /// <param name="fhirSerializer">FHIR JSON serializer.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EpicFhirContext(
        HttpClient httpClient,
        IFhirSerializer fhirSerializer,
        ILogger<EpicFhirContext<TResource>> logger)
    {
        _httpClient = httpClient;
        _fhirSerializer = fhirSerializer;
        _logger = logger;
        _resourceType = typeof(TResource).Name;
    }

    /// <inheritdoc />
    public async Task<Result<TResource>> ReadAsync(string id, string accessToken, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_resourceType}/{id}");
            ConfigureRequest(request, accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return FhirErrors.NotFound(_resourceType, id);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return FhirErrors.AuthenticationFailed;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var resource = _fhirSerializer.Deserialize<TResource>(json);

            if (resource is null)
            {
                return FhirErrors.InvalidResponse($"Failed to deserialize {_resourceType}/{id}");
            }

            return resource;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reading {ResourceType}/{Id}", _resourceType, id);
            return FhirErrors.NetworkError(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TResource>>> SearchAsync(
        string query,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_resourceType}?{query}");
            ConfigureRequest(request, accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<IReadOnlyList<TResource>>.Failure(FhirErrors.AuthenticationFailed);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var bundle = _fhirSerializer.DeserializeBundle(json);
            var resources = ExtractResourcesFromBundle(bundle);

            return Result<IReadOnlyList<TResource>>.Success(resources);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error searching {ResourceType}", _resourceType);
            return Result<IReadOnlyList<TResource>>.Failure(FhirErrors.NetworkError(ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TResource>> CreateAsync(
        TResource resource,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _resourceType);
            ConfigureRequest(request, accessToken);

            var jsonContent = _fhirSerializer.Serialize(resource);
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/fhir+json");

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return FhirErrors.AuthenticationFailed;
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return ErrorFactory.Validation(error);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var created = _fhirSerializer.Deserialize<TResource>(json);

            if (created is null)
            {
                return FhirErrors.InvalidResponse($"Failed to deserialize created {_resourceType}");
            }

            return created;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating {ResourceType}", _resourceType);
            return FhirErrors.NetworkError(ex.Message, ex);
        }
    }

    private static void ConfigureRequest(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    }

    private IReadOnlyList<TResource> ExtractResourcesFromBundle(Bundle? bundle)
    {
        if (bundle?.Entry is null)
        {
            return [];
        }

        return bundle.Entry
            .Where(e => e.Resource is TResource)
            .Select(e => (TResource)e.Resource)
            .ToList();
    }
}
