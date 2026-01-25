using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Gateway.API.Services;

/// <summary>
/// HTTP client implementation for uploading documents to Epic's FHIR server.
/// Creates FHIR DocumentReference resources with embedded PDF content.
/// </summary>
public sealed class EpicUploader : IEpicUploader
{
    private readonly IEpicFhirClient _fhirClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<EpicUploader> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicUploader"/> class.
    /// </summary>
    /// <param name="fhirClient">The Epic FHIR client for reference.</param>
    /// <param name="httpClient">HTTP client for direct FHIR calls.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="configuration">Configuration for Epic FHIR base URL.</param>
    public EpicUploader(
        IEpicFhirClient fhirClient,
        HttpClient httpClient,
        ILogger<EpicUploader> logger,
        IConfiguration configuration)
    {
        _fhirClient = fhirClient;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<string> UploadDocumentAsync(
        byte[] pdfBytes,
        string patientId,
        string? encounterId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Uploading PA form to Epic. PatientId={PatientId}, Size={Size} bytes",
            patientId, pdfBytes.Length);

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

        var baseUrl = _configuration["Epic:FhirBaseUrl"]
            ?? "https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/DocumentReference");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to upload document: {Status} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Epic returned {response.StatusCode}: {error}");
        }

        var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var documentId = responseJson.TryGetProperty("id", out var id)
            ? id.GetString()
            : Guid.NewGuid().ToString();

        _logger.LogInformation("Document uploaded successfully. DocumentId={DocumentId}", documentId);

        return documentId!;
    }
}
