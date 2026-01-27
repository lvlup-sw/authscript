using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Fhir;

namespace Gateway.API.Services.Fhir;

/// <summary>
/// Generic FHIR context implementation for Epic's FHIR R4 API.
/// Provides low-level CRUD operations with Result-based error handling.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public class EpicFhirContext<TResource> : IFhirContext<TResource> where TResource : class
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EpicFhirContext<TResource>> _logger;
    private readonly string _resourceType;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicFhirContext{TResource}"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with Epic FHIR base URL.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EpicFhirContext(HttpClient httpClient, ILogger<EpicFhirContext<TResource>> logger)
    {
        _httpClient = httpClient;
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
                return Result<TResource>.Failure(FhirError.NotFound(_resourceType, id));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<TResource>.Failure(FhirError.Unauthorized());
            }

            response.EnsureSuccessStatusCode();

            var resource = await response.Content.ReadFromJsonAsync<TResource>(cancellationToken: ct);

            if (resource is null)
            {
                return Result<TResource>.Failure(
                    FhirError.Validation($"Failed to deserialize {_resourceType}/{id}"));
            }

            return Result<TResource>.Success(resource);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reading {ResourceType}/{Id}", _resourceType, id);
            return Result<TResource>.Failure(FhirError.Network(ex.Message, ex));
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

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<IReadOnlyList<TResource>>.Failure(
                    FhirError.InvalidResponse($"FHIR {_resourceType} search endpoint not found"));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<IReadOnlyList<TResource>>.Failure(FhirError.Unauthorized());
            }

            response.EnsureSuccessStatusCode();

            var bundle = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var resources = ExtractResourcesFromBundle(bundle);

            return Result<IReadOnlyList<TResource>>.Success(resources);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error searching {ResourceType}", _resourceType);
            return Result<IReadOnlyList<TResource>>.Failure(FhirError.Network(ex.Message, ex));
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
            request.Content = JsonContent.Create(resource);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<TResource>.Failure(FhirError.Unauthorized());
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return Result<TResource>.Failure(FhirError.Validation(error));
            }

            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<TResource>(cancellationToken: ct);

            if (created is null)
            {
                return Result<TResource>.Failure(
                    FhirError.Validation($"Failed to deserialize created {_resourceType}"));
            }

            return Result<TResource>.Success(created);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating {ResourceType}", _resourceType);
            return Result<TResource>.Failure(FhirError.Network(ex.Message, ex));
        }
    }

    private static void ConfigureRequest(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    }

    private IReadOnlyList<TResource> ExtractResourcesFromBundle(JsonElement bundle)
    {
        var results = new List<TResource>();

        if (!bundle.TryGetProperty("entry", out var entries))
        {
            return results;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.TryGetProperty("resource", out var resource))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<TResource>(resource.GetRawText());
                    if (parsed is not null)
                    {
                        results.Add(parsed);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize resource in bundle");
                }
            }
        }

        return results;
    }
}
