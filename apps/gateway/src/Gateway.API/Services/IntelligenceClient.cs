using System.Net.Http.Json;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// HTTP client implementation for the Intelligence service.
/// Transforms clinical data into the format expected by the AI analysis endpoint.
/// </summary>
public sealed class IntelligenceClient : IIntelligenceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IntelligenceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntelligenceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with Intelligence service base URL.</param>
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
            "Sending analysis request. PatientId={PatientId}, ProcedureCode={ProcedureCode}",
            clinicalBundle.PatientId, procedureCode);

        var request = new
        {
            patient_id = clinicalBundle.PatientId,
            procedure_code = procedureCode,
            clinical_data = new
            {
                patient = clinicalBundle.Patient is not null
                    ? new
                    {
                        name = clinicalBundle.Patient.FullName,
                        birth_date = clinicalBundle.Patient.BirthDate?.ToString("yyyy-MM-dd"),
                        gender = clinicalBundle.Patient.Gender,
                        member_id = clinicalBundle.Patient.MemberId
                    }
                    : null,
                conditions = clinicalBundle.Conditions.Select(c => new
                {
                    code = c.Code,
                    system = c.CodeSystem,
                    display = c.Display,
                    clinical_status = c.ClinicalStatus
                }),
                observations = clinicalBundle.Observations.Select(o => new
                {
                    code = o.Code,
                    system = o.CodeSystem,
                    display = o.Display,
                    value = o.Value,
                    unit = o.Unit
                }),
                procedures = clinicalBundle.Procedures.Select(p => new
                {
                    code = p.Code,
                    system = p.CodeSystem,
                    display = p.Display,
                    status = p.Status
                })
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/analyze", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Intelligence service error: {Status} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Intelligence service returned {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<PAFormData>(cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Intelligence service returned null response");
        }

        _logger.LogInformation(
            "Analysis complete. Recommendation={Recommendation}, Confidence={Confidence}",
            result.Recommendation, result.ConfidenceScore);

        return result;
    }
}
