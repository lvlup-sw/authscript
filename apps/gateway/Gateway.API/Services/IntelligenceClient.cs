using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// HTTP client that calls the Intelligence service (Python/FastAPI) for PA analysis.
/// </summary>
public sealed class IntelligenceClient : IIntelligenceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IntelligenceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="IntelligenceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public IntelligenceClient(HttpClient httpClient, ILogger<IntelligenceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PAFormData> AnalyzeAsync(
        ClinicalBundle clinicalBundle,
        string procedureCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calling Intelligence service for ProcedureCode={ProcedureCode}, PatientId={PatientId}",
            procedureCode, clinicalBundle.PatientId);

        var requestBody = BuildRequestPayload(clinicalBundle, procedureCode);

        var response = await _httpClient.PostAsJsonAsync(
            "/analyze", requestBody, JsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PAFormData>(cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Intelligence service returned null response");
        }

        _logger.LogInformation(
            "Intelligence analysis complete: Recommendation={Recommendation}, Confidence={Confidence}",
            result.Recommendation, result.ConfidenceScore);

        return result;
    }

    private static object BuildRequestPayload(ClinicalBundle clinicalBundle, string procedureCode)
    {
        var clinicalData = new Dictionary<string, object?>();

        if (clinicalBundle.Patient is not null)
        {
            clinicalData["patient"] = new Dictionary<string, object?>
            {
                ["name"] = clinicalBundle.Patient.FullName,
                ["birth_date"] = clinicalBundle.Patient.BirthDate?.ToString("yyyy-MM-dd"),
                ["gender"] = clinicalBundle.Patient.Gender,
                ["member_id"] = clinicalBundle.Patient.MemberId,
            };
        }

        clinicalData["conditions"] = clinicalBundle.Conditions.Select(c => new Dictionary<string, object?>
        {
            ["code"] = c.Code,
            ["system"] = c.CodeSystem,
            ["display"] = c.Display,
            ["clinical_status"] = c.ClinicalStatus,
        }).ToList();

        clinicalData["observations"] = clinicalBundle.Observations.Select(o => new Dictionary<string, object?>
        {
            ["code"] = o.Code,
            ["system"] = o.CodeSystem,
            ["display"] = o.Display,
            ["value"] = o.Value,
            ["unit"] = o.Unit,
        }).ToList();

        clinicalData["procedures"] = clinicalBundle.Procedures.Select(p => new Dictionary<string, object?>
        {
            ["code"] = p.Code,
            ["system"] = p.CodeSystem,
            ["display"] = p.Display,
            ["status"] = p.Status,
        }).ToList();

        return new
        {
            patient_id = clinicalBundle.PatientId,
            procedure_code = procedureCode,
            clinical_data = clinicalData,
        };
    }
}
