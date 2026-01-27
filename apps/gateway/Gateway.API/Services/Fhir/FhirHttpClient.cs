using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gateway.API.Contracts;

namespace Gateway.API.Services.Fhir;

/// <summary>
/// HTTP client implementation for FHIR R4 API operations.
/// Handles authentication, request formatting, and response handling.
/// </summary>
public sealed class FhirHttpClient : IFhirHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FhirHttpClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with FHIR base URL.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirHttpClient(HttpClient httpClient, ILogger<FhirHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<JsonElement>> ReadAsync(
        string resourceType,
        string id,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{resourceType}/{id}");
            ConfigureRequest(request, accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<JsonElement>.Failure(FhirError.NotFound(resourceType, id));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<JsonElement>.Failure(FhirError.Unauthorized());
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return Result<JsonElement>.Success(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reading {ResourceType}/{Id}", resourceType, id);
            return Result<JsonElement>.Failure(FhirError.Network(ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<JsonElement>> SearchAsync(
        string resourceType,
        string query,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{resourceType}?{query}");
            ConfigureRequest(request, accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<JsonElement>.Failure(FhirError.Unauthorized());
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return Result<JsonElement>.Success(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error searching {ResourceType}", resourceType);
            return Result<JsonElement>.Failure(FhirError.Network(ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<JsonElement>> CreateAsync(
        string resourceType,
        string resourceJson,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, resourceType);
            ConfigureRequest(request, accessToken);
            request.Content = new StringContent(resourceJson, Encoding.UTF8, "application/fhir+json");

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<JsonElement>.Failure(FhirError.Unauthorized());
            }

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return Result<JsonElement>.Failure(FhirError.Validation(error));
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return Result<JsonElement>.Success(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating {ResourceType}", resourceType);
            return Result<JsonElement>.Failure(FhirError.Network(ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> ReadBinaryAsync(
        string id,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"Binary/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<byte[]>.Failure(FhirError.NotFound("Binary", id));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<byte[]>.Failure(FhirError.Unauthorized());
            }

            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return Result<byte[]>.Success(bytes);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reading Binary/{Id}", id);
            return Result<byte[]>.Failure(FhirError.Network(ex.Message, ex));
        }
    }

    private static void ConfigureRequest(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    }
}
