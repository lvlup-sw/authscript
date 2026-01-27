using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Microsoft.Extensions.Options;

namespace Gateway.API.Services;

/// <summary>
/// Uploads documents to a FHIR server as DocumentReference resources.
/// Uses IFhirHttpClient for HTTP operations.
/// </summary>
public sealed class DocumentUploader : IDocumentUploader
{
    private readonly IFhirHttpClient _fhirHttpClient;
    private readonly ILogger<DocumentUploader> _logger;
    private readonly DocumentOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentUploader"/> class.
    /// </summary>
    /// <param name="fhirHttpClient">Low-level FHIR HTTP client.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="options">Document configuration options.</param>
    public DocumentUploader(
        IFhirHttpClient fhirHttpClient,
        ILogger<DocumentUploader> logger,
        IOptions<DocumentOptions> options)
    {
        _fhirHttpClient = fhirHttpClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Result<string>> UploadDocumentAsync(
        byte[] pdfBytes,
        string patientId,
        string? encounterId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Uploading PA form. Size={Size} bytes",
            pdfBytes.Length);

        var documentReference = BuildDocumentReference(pdfBytes, patientId, encounterId);
        var json = JsonSerializer.Serialize(documentReference);

        var result = await _fhirHttpClient.CreateAsync("DocumentReference", json, accessToken, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError(
                "Failed to upload document: {Error}",
                result.Error?.Message);
            return Result<string>.Failure(result.Error!);
        }

        var responseJson = result.Value!;
        string documentId;
        if (!responseJson.TryGetProperty("id", out var id) || string.IsNullOrEmpty(id.GetString()))
        {
            _logger.LogWarning("FHIR server response missing document ID, generating synthetic ID");
            documentId = Guid.NewGuid().ToString();
        }
        else
        {
            documentId = id.GetString()!;
        }

        _logger.LogInformation("Document uploaded successfully. DocumentId={DocumentId}", documentId);

        return Result<string>.Success(documentId);
    }

    private object BuildDocumentReference(byte[] pdfBytes, string patientId, string? encounterId)
    {
        return new
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
                        code = _options.PriorAuthLoincCode,
                        display = _options.PriorAuthLoincDisplay
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
                        title = $"PA Form - {DateTime.UtcNow:yyyy-MM-dd}"
                    }
                }
            }
        };
    }
}
