using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gateway.API.Contracts;

namespace Gateway.API.Services.Fhir;

/// <summary>
/// HTTP client implementation for FHIR R4 API operations.
/// Handles authentication, request formatting, and response handling.
/// Token management is handled internally via IFhirTokenProvider.
/// </summary>
public sealed class FhirHttpClient : IFhirHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IFhirTokenProvider _tokenProvider;
    private readonly ILogger<FhirHttpClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with FHIR base URL.</param>
    /// <param name="tokenProvider">Provider for FHIR access tokens.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirHttpClient(HttpClient httpClient, IFhirTokenProvider tokenProvider, ILogger<FhirHttpClient> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<JsonElement>> ReadAsync(
        string resourceType,
        string id,
        CancellationToken ct = default)
    {
        try
        {
            var accessToken = await _tokenProvider.GetTokenAsync(ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{resourceType}/{id}");
            ConfigureRequest(request, accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            var error = HttpResponseErrorFactory.ValidateReadResponse<JsonElement>(response, resourceType, id);
            if (error is not null) return error.Value;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return Result<JsonElement>.Success(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reading {ResourceType}/{Id}", resourceType, id);
            return HttpResponseErrorFactory.NetworkError<JsonElement>(ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON response reading {ResourceType}/{Id}", resourceType, id);
            return HttpResponseErrorFactory.JsonError<JsonElement>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<JsonElement>> SearchAsync(
        string resourceType,
        string query,
        CancellationToken ct = default)
    {
        try
        {
            var accessToken = await _tokenProvider.GetTokenAsync(ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{resourceType}?{query}");
            ConfigureRequest(request, accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            var error = HttpResponseErrorFactory.ValidateSearchResponse<JsonElement>(response, resourceType);
            if (error is not null) return error.Value;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return Result<JsonElement>.Success(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error searching {ResourceType}", resourceType);
            return HttpResponseErrorFactory.NetworkError<JsonElement>(ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON response searching {ResourceType}", resourceType);
            return HttpResponseErrorFactory.JsonError<JsonElement>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<JsonElement>> CreateAsync(
        string resourceType,
        string resourceJson,
        CancellationToken ct = default)
    {
        try
        {
            var accessToken = await _tokenProvider.GetTokenAsync(ct);

            using var request = new HttpRequestMessage(HttpMethod.Post, resourceType);
            ConfigureRequest(request, accessToken);
            request.Content = new StringContent(resourceJson, Encoding.UTF8, "application/fhir+json");

            var response = await _httpClient.SendAsync(request, ct);

            string? validationError = null;
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                validationError = await response.Content.ReadAsStringAsync(ct);
            }

            var error = HttpResponseErrorFactory.ValidateCreateResponse<JsonElement>(response, validationError);
            if (error is not null) return error.Value;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return Result<JsonElement>.Success(json);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating {ResourceType}", resourceType);
            return HttpResponseErrorFactory.NetworkError<JsonElement>(ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON response creating {ResourceType}", resourceType);
            return HttpResponseErrorFactory.JsonError<JsonElement>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> ReadBinaryAsync(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            var accessToken = await _tokenProvider.GetTokenAsync(ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"Binary/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            var error = HttpResponseErrorFactory.ValidateReadResponse<byte[]>(response, "Binary", id);
            if (error is not null) return error.Value;

            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return Result<byte[]>.Success(bytes);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error reading Binary/{Id}", id);
            return HttpResponseErrorFactory.NetworkError<byte[]>(ex);
        }
    }

    private static void ConfigureRequest(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    }
}
