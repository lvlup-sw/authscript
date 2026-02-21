using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Intelligence client that calls the Python Intelligence service HTTP API
/// to analyze clinical data and generate prior authorization form data.
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
            "Calling Intelligence service for PatientId={PatientId}, ProcedureCode={ProcedureCode}",
            clinicalBundle.PatientId, procedureCode);

        var requestDto = BuildAnalyzeRequest(clinicalBundle, procedureCode);

        var response = await _httpClient.PostAsJsonAsync(
            "/api/analyze",
            requestDto,
            SnakeCaseSerializerOptions,
            cancellationToken);

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

    internal static readonly JsonSerializerOptions SnakeCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static AnalyzeRequestDto BuildAnalyzeRequest(ClinicalBundle bundle, string procedureCode)
    {
        var patient = bundle.Patient is not null
            ? new PatientDto(
                Name: bundle.Patient.FullName,
                BirthDate: bundle.Patient.BirthDate?.ToString("yyyy-MM-dd"),
                Gender: bundle.Patient.Gender,
                MemberId: bundle.Patient.MemberId)
            : null;

        var conditions = bundle.Conditions.Select(c => new ConditionDto(
            Code: c.Code, System: c.CodeSystem, Display: c.Display, ClinicalStatus: c.ClinicalStatus)).ToList();

        var observations = bundle.Observations.Select(o => new ObservationDto(
            Code: o.Code, System: o.CodeSystem, Display: o.Display, Value: o.Value, Unit: o.Unit)).ToList();

        var procedures = bundle.Procedures.Select(p => new ProcedureDto(
            Code: p.Code, System: p.CodeSystem, Display: p.Display, Status: p.Status)).ToList();

        return new AnalyzeRequestDto(
            PatientId: bundle.PatientId,
            ProcedureCode: procedureCode,
            ClinicalData: new ClinicalDataDto(patient, conditions, observations, procedures));
    }
}

internal sealed record AnalyzeRequestDto(string PatientId, string ProcedureCode, ClinicalDataDto ClinicalData);

internal sealed record ClinicalDataDto(
    PatientDto? Patient,
    List<ConditionDto> Conditions,
    List<ObservationDto> Observations,
    List<ProcedureDto> Procedures);

internal sealed record PatientDto(string Name, string? BirthDate, string? Gender, string? MemberId);
internal sealed record ConditionDto(string Code, string? System, string? Display, string? ClinicalStatus);
internal sealed record ObservationDto(string Code, string? System, string? Display, string? Value, string? Unit);
internal sealed record ProcedureDto(string Code, string? System, string? Display, string? Status);
