using System.Net;
using Gateway.API.Abstractions;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Contracts.Http;
using Gateway.API.Errors;
using Gateway.API.Models;
using Hl7.Fhir.Model;

namespace Gateway.API.Services;

/// <summary>
/// HTTP client implementation for Epic's FHIR R4 API.
/// Uses IHttpClientProvider for authentication and IFhirSerializer for parsing.
/// </summary>
public sealed class EpicFhirClient : IEpicFhirClient
{
    private const string ClientName = "EpicFhir";

    private readonly IHttpClientProvider _httpClientProvider;
    private readonly IFhirSerializer _fhirSerializer;
    private readonly ILogger<EpicFhirClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicFhirClient"/> class.
    /// </summary>
    /// <param name="httpClientProvider">Provider for authenticated HTTP clients.</param>
    /// <param name="fhirSerializer">FHIR JSON serializer.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EpicFhirClient(
        IHttpClientProvider httpClientProvider,
        IFhirSerializer fhirSerializer,
        ILogger<EpicFhirClient> logger)
    {
        _httpClientProvider = httpClientProvider;
        _fhirSerializer = fhirSerializer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PatientInfo>> GetPatientAsync(
        string patientId,
        CancellationToken ct = default)
    {
        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var response = await httpClient.GetAsync($"Patient/{patientId}", ct);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ParsePatientAsync(response, patientId, ct),
            HttpStatusCode.NotFound => FhirErrors.NotFound("Patient", patientId),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => FhirErrors.AuthenticationFailed,
            _ => FhirErrors.NetworkError($"FHIR server returned {response.StatusCode}")
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ConditionInfo>>> SearchConditionsAsync(
        string patientId,
        CancellationToken ct = default)
    {
        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var response = await httpClient.GetAsync(
            $"Condition?patient={patientId}&clinical-status=active", ct);

        if (!response.IsSuccessStatusCode)
        {
            return MapHttpError(response.StatusCode, "Condition search");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var bundle = _fhirSerializer.DeserializeBundle(json);

        return MapConditions(bundle);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ObservationInfo>>> SearchObservationsAsync(
        string patientId,
        DateOnly since,
        CancellationToken ct = default)
    {
        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var response = await httpClient.GetAsync(
            $"Observation?patient={patientId}&category=laboratory&date=ge{since:yyyy-MM-dd}", ct);

        if (!response.IsSuccessStatusCode)
        {
            return MapHttpError(response.StatusCode, "Observation search");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var bundle = _fhirSerializer.DeserializeBundle(json);

        return MapObservations(bundle);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProcedureInfo>>> SearchProceduresAsync(
        string patientId,
        DateOnly since,
        CancellationToken ct = default)
    {
        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var response = await httpClient.GetAsync(
            $"Procedure?patient={patientId}&date=ge{since:yyyy-MM-dd}", ct);

        if (!response.IsSuccessStatusCode)
        {
            return MapHttpError(response.StatusCode, "Procedure search");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var bundle = _fhirSerializer.DeserializeBundle(json);

        return MapProcedures(bundle);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentInfo>>> SearchDocumentsAsync(
        string patientId,
        CancellationToken ct = default)
    {
        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var response = await httpClient.GetAsync(
            $"DocumentReference?patient={patientId}&status=current", ct);

        if (!response.IsSuccessStatusCode)
        {
            return MapHttpError(response.StatusCode, "DocumentReference search");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var bundle = _fhirSerializer.DeserializeBundle(json);

        return MapDocuments(bundle);
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> GetDocumentContentAsync(
        string documentId,
        CancellationToken ct = default)
    {
        var httpClient = await _httpClientProvider.GetAuthenticatedClientAsync(ClientName, ct);
        if (httpClient is null)
        {
            return FhirErrors.AuthenticationFailed;
        }

        var response = await httpClient.GetAsync($"Binary/{documentId}", ct);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadAsByteArrayAsync(ct),
            HttpStatusCode.NotFound => FhirErrors.NotFound("Binary", documentId),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => FhirErrors.AuthenticationFailed,
            _ => FhirErrors.NetworkError($"FHIR server returned {response.StatusCode}")
        };
    }

    private async Task<Result<PatientInfo>> ParsePatientAsync(
        HttpResponseMessage response,
        string patientId,
        CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        var patient = _fhirSerializer.Deserialize<Patient>(json);

        if (patient is null)
        {
            return FhirErrors.InvalidResponse("Failed to parse Patient resource");
        }

        return new PatientInfo
        {
            Id = patient.Id ?? patientId,
            GivenName = ExtractGivenName(patient),
            FamilyName = ExtractFamilyName(patient),
            BirthDate = ParseDate(patient.BirthDate),
            Gender = patient.Gender?.ToString()
        };
    }

    private static string? ExtractGivenName(Patient patient)
    {
        var name = patient.Name.FirstOrDefault();
        if (name?.Given is null) return null;
        return string.Join(" ", name.Given);
    }

    private static string? ExtractFamilyName(Patient patient)
    {
        return patient.Name.FirstOrDefault()?.Family;
    }

    private static DateOnly? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        if (DateOnly.TryParse(dateStr, out var date)) return date;
        return null;
    }

    private static Result<IReadOnlyList<ConditionInfo>> MapConditions(Bundle? bundle)
    {
        if (bundle is null)
        {
            return Array.Empty<ConditionInfo>();
        }

        var conditions = new List<ConditionInfo>();

        foreach (var entry in bundle.Entry ?? Enumerable.Empty<Bundle.EntryComponent>())
        {
            if (entry.Resource is Condition condition && condition.Code?.Coding?.Count > 0)
            {
                var coding = condition.Code.Coding[0];
                conditions.Add(new ConditionInfo
                {
                    Id = condition.Id ?? Guid.NewGuid().ToString(),
                    Code = coding.Code,
                    CodeSystem = coding.System,
                    Display = coding.Display ?? condition.Code.Text,
                    ClinicalStatus = ExtractClinicalStatus(condition)
                });
            }
        }

        return conditions;
    }

    private static string? ExtractClinicalStatus(Condition condition)
    {
        return condition.ClinicalStatus?.Coding?.FirstOrDefault()?.Code;
    }

    private static Result<IReadOnlyList<ObservationInfo>> MapObservations(Bundle? bundle)
    {
        if (bundle is null)
        {
            return Array.Empty<ObservationInfo>();
        }

        var observations = new List<ObservationInfo>();

        foreach (var entry in bundle.Entry ?? Enumerable.Empty<Bundle.EntryComponent>())
        {
            if (entry.Resource is Observation obs && obs.Code?.Coding?.Count > 0)
            {
                var coding = obs.Code.Coding[0];
                observations.Add(new ObservationInfo
                {
                    Id = obs.Id ?? Guid.NewGuid().ToString(),
                    Code = coding.Code,
                    CodeSystem = coding.System,
                    Display = coding.Display ?? obs.Code.Text,
                    Value = ExtractObservationValue(obs),
                    Unit = ExtractObservationUnit(obs)
                });
            }
        }

        return observations;
    }

    private static string? ExtractObservationValue(Observation obs)
    {
        return obs.Value switch
        {
            Quantity q => q.Value?.ToString(),
            FhirString s => s.Value,
            _ => null
        };
    }

    private static string? ExtractObservationUnit(Observation obs)
    {
        return obs.Value is Quantity q ? q.Unit : null;
    }

    private static Result<IReadOnlyList<ProcedureInfo>> MapProcedures(Bundle? bundle)
    {
        if (bundle is null)
        {
            return Array.Empty<ProcedureInfo>();
        }

        var procedures = new List<ProcedureInfo>();

        foreach (var entry in bundle.Entry ?? Enumerable.Empty<Bundle.EntryComponent>())
        {
            if (entry.Resource is Procedure proc && proc.Code?.Coding?.Count > 0)
            {
                var coding = proc.Code.Coding[0];
                procedures.Add(new ProcedureInfo
                {
                    Id = proc.Id ?? Guid.NewGuid().ToString(),
                    Code = coding.Code,
                    CodeSystem = coding.System,
                    Display = coding.Display ?? proc.Code.Text,
                    Status = proc.Status?.ToString()
                });
            }
        }

        return procedures;
    }

    private static Result<IReadOnlyList<DocumentInfo>> MapDocuments(Bundle? bundle)
    {
        if (bundle is null)
        {
            return Array.Empty<DocumentInfo>();
        }

        var documents = new List<DocumentInfo>();

        foreach (var entry in bundle.Entry ?? Enumerable.Empty<Bundle.EntryComponent>())
        {
            if (entry.Resource is DocumentReference docRef)
            {
                var typeCoding = docRef.Type?.Coding?.FirstOrDefault();
                var attachment = docRef.Content?.FirstOrDefault()?.Attachment;

                documents.Add(new DocumentInfo
                {
                    Id = docRef.Id ?? Guid.NewGuid().ToString(),
                    Type = typeCoding?.Display ?? typeCoding?.Code ?? "Unknown",
                    ContentType = attachment?.ContentType,
                    Title = attachment?.Title
                });
            }
        }

        return documents;
    }

    private static Error MapHttpError(HttpStatusCode statusCode, string operation)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => FhirErrors.NotFound(operation, "search"),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => FhirErrors.AuthenticationFailed,
            HttpStatusCode.ServiceUnavailable => FhirErrors.ServiceUnavailable,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => FhirErrors.Timeout,
            _ => FhirErrors.NetworkError($"FHIR {operation} failed with status {statusCode}")
        };
    }
}
