using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Gateway.API.Abstractions;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Gateway.API.Errors;

namespace Gateway.API.Services;

/// <summary>
/// HTTP client implementation for uploading documents to Epic's FHIR server.
/// Creates FHIR DocumentReference resources with embedded PDF content.
/// </summary>
public sealed class EpicUploader : IEpicUploader
{
    private const string ClientName = "EpicFhir";

    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ILogger<EpicUploader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicUploader"/> class.
    /// </summary>
    /// <param name="httpClientProvider">Provider for authenticated HTTP clients.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EpicUploader(
        IHttpClientProvider httpClientProvider,
        ILogger<EpicUploader> logger)
    {
        _httpClientProvider = httpClientProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> UploadDocumentAsync(
        byte[] pdfBytes,
        string patientId,
        string? encounterId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Uploading PA form to Epic. PatientId={PatientId}, Size={Size} bytes",
            patientId, pdfBytes.Length);

        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var documentReference = new
        {
            resourceType = "DocumentReference",
            status = "current",
            type = new
            {
                coding = new[]
                {
                    new
                    {
                        system = "http://loinc.org",
                        code = "64289-6",
                        display = "Prior authorization request"
                    }
                }
            },
            subject = new
            {
                reference = $"Patient/{patientId}"
            },
            context = encounterId is not null
                ? new
                {
                    encounter = new[]
                    {
                        new { reference = $"Encounter/{encounterId}" }
                    }
                }
                : null,
            content = new[]
            {
                new
                {
                    attachment = new
                    {
                        contentType = "application/pdf",
                        data = Convert.ToBase64String(pdfBytes),
                        title = $"AuthScript PA Form - {DateTime.UtcNow:yyyy-MM-dd}"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(documentReference);
        var content = new StringContent(json, Encoding.UTF8, "application/fhir+json");

        try
        {
            var response = await httpClient.PostAsync("DocumentReference", content, ct);

            return response.StatusCode switch
            {
                HttpStatusCode.Created or HttpStatusCode.OK => await ExtractDocumentIdAsync(response, ct),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => FhirErrors.AuthenticationFailed,
                HttpStatusCode.UnprocessableEntity => await ExtractValidationErrorAsync(response, ct),
                _ => await ExtractGenericErrorAsync(response, ct)
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error uploading document");
            return FhirErrors.NetworkError($"Epic upload failed: {ex.Message}", ex);
        }
    }

    private async Task<Result<string>> ExtractDocumentIdAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var documentId = responseJson.TryGetProperty("id", out var id)
            ? id.GetString()
            : Guid.NewGuid().ToString();

        _logger.LogInformation("Document uploaded successfully. DocumentId={DocumentId}", documentId);

        return documentId!;
    }

    private async Task<Error> ExtractValidationErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Validation error uploading document: {Error}", error);
        return ErrorFactory.Validation($"Epic rejected document: {error}");
    }

    private async Task<Error> ExtractGenericErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var error = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Failed to upload document: {Status} - {Error}", response.StatusCode, error);
        return FhirErrors.NetworkError($"Epic returned {response.StatusCode}: {error}");
    }
}
