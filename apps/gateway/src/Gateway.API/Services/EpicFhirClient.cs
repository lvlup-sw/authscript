using System.Net.Http.Headers;
using System.Text.Json;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// HTTP client implementation for Epic's FHIR R4 API.
/// Handles authentication, request formatting, and response parsing.
/// </summary>
public sealed class EpicFhirClient : IEpicFhirClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EpicFhirClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpicFhirClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with Epic's FHIR base URL.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EpicFhirClient(HttpClient httpClient, ILogger<EpicFhirClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PatientInfo?> GetPatientAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"Patient/{patientId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch patient {PatientId}: {Status}", patientId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        return new PatientInfo
        {
            Id = patientId,
            GivenName = ExtractName(json, "given"),
            FamilyName = ExtractName(json, "family"),
            BirthDate = ExtractDate(json, "birthDate"),
            Gender = json.TryGetProperty("gender", out var gender) ? gender.GetString() : null
        };
    }

    /// <inheritdoc />
    public async Task<List<ConditionInfo>> SearchConditionsAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ConditionInfo>();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"Condition?patient={patientId}&clinical-status=active");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to search conditions for {PatientId}: {Status}", patientId, response.StatusCode);
            return results;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (json.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("resource", out var resource))
                {
                    var coding = ExtractFirstCoding(resource, "code");
                    if (coding is not null)
                    {
                        results.Add(new ConditionInfo
                        {
                            Id = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
                            Code = coding.Value.code,
                            CodeSystem = coding.Value.system,
                            Display = coding.Value.display,
                            ClinicalStatus = ExtractClinicalStatus(resource)
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<List<ObservationInfo>> SearchObservationsAsync(
        string patientId,
        DateOnly since,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ObservationInfo>();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"Observation?patient={patientId}&category=laboratory&date=ge{since:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to search observations for {PatientId}: {Status}", patientId, response.StatusCode);
            return results;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (json.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("resource", out var resource))
                {
                    var coding = ExtractFirstCoding(resource, "code");
                    if (coding is not null)
                    {
                        results.Add(new ObservationInfo
                        {
                            Id = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
                            Code = coding.Value.code,
                            CodeSystem = coding.Value.system,
                            Display = coding.Value.display,
                            Value = ExtractObservationValue(resource),
                            Unit = ExtractObservationUnit(resource)
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<List<ProcedureInfo>> SearchProceduresAsync(
        string patientId,
        DateOnly since,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcedureInfo>();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"Procedure?patient={patientId}&date=ge{since:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to search procedures for {PatientId}: {Status}", patientId, response.StatusCode);
            return results;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (json.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("resource", out var resource))
                {
                    var coding = ExtractFirstCoding(resource, "code");
                    if (coding is not null)
                    {
                        results.Add(new ProcedureInfo
                        {
                            Id = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
                            Code = coding.Value.code,
                            CodeSystem = coding.Value.system,
                            Display = coding.Value.display,
                            Status = resource.TryGetProperty("status", out var status) ? status.GetString() : null
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<List<DocumentInfo>> SearchDocumentsAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DocumentInfo>();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"DocumentReference?patient={patientId}&status=current");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to search documents for {PatientId}: {Status}", patientId, response.StatusCode);
            return results;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (json.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("resource", out var resource))
                {
                    var docId = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString();
                    var type = ExtractFirstCoding(resource, "type");

                    results.Add(new DocumentInfo
                    {
                        Id = docId,
                        Type = type?.display ?? type?.code ?? "Unknown",
                        ContentType = ExtractContentType(resource),
                        Title = ExtractDocumentTitle(resource)
                    });
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetDocumentContentAsync(
        string documentId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"Binary/{documentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch document content {DocumentId}: {Status}", documentId, response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static string? ExtractName(JsonElement json, string part)
    {
        if (!json.TryGetProperty("name", out var names)) return null;

        foreach (var name in names.EnumerateArray())
        {
            if (part == "given" && name.TryGetProperty("given", out var given))
            {
                var givenNames = new List<string>();
                foreach (var g in given.EnumerateArray())
                {
                    givenNames.Add(g.GetString() ?? "");
                }
                return string.Join(" ", givenNames);
            }
            if (part == "family" && name.TryGetProperty("family", out var family))
            {
                return family.GetString();
            }
        }

        return null;
    }

    private static DateOnly? ExtractDate(JsonElement json, string property)
    {
        if (!json.TryGetProperty(property, out var value)) return null;
        if (DateOnly.TryParse(value.GetString(), out var date)) return date;
        return null;
    }

    private static (string code, string? system, string? display)? ExtractFirstCoding(JsonElement json, string property)
    {
        if (!json.TryGetProperty(property, out var codeableConcept)) return null;
        if (!codeableConcept.TryGetProperty("coding", out var codings)) return null;

        foreach (var coding in codings.EnumerateArray())
        {
            var code = coding.TryGetProperty("code", out var c) ? c.GetString() : null;
            if (code is null) continue;

            var system = coding.TryGetProperty("system", out var s) ? s.GetString() : null;
            var display = coding.TryGetProperty("display", out var d) ? d.GetString() : null;

            return (code, system, display);
        }

        return null;
    }

    private static string? ExtractClinicalStatus(JsonElement resource)
    {
        if (!resource.TryGetProperty("clinicalStatus", out var status)) return null;
        var coding = ExtractFirstCoding(status, "coding");
        return coding?.code;
    }

    private static string? ExtractObservationValue(JsonElement resource)
    {
        if (resource.TryGetProperty("valueQuantity", out var quantity))
        {
            return quantity.TryGetProperty("value", out var v) ? v.ToString() : null;
        }
        if (resource.TryGetProperty("valueString", out var str))
        {
            return str.GetString();
        }
        return null;
    }

    private static string? ExtractObservationUnit(JsonElement resource)
    {
        if (!resource.TryGetProperty("valueQuantity", out var quantity)) return null;
        return quantity.TryGetProperty("unit", out var unit) ? unit.GetString() : null;
    }

    private static string? ExtractContentType(JsonElement resource)
    {
        if (!resource.TryGetProperty("content", out var contents)) return null;
        foreach (var content in contents.EnumerateArray())
        {
            if (content.TryGetProperty("attachment", out var attachment))
            {
                if (attachment.TryGetProperty("contentType", out var ct))
                {
                    return ct.GetString();
                }
            }
        }
        return null;
    }

    private static string? ExtractDocumentTitle(JsonElement resource)
    {
        if (!resource.TryGetProperty("content", out var contents)) return null;
        foreach (var content in contents.EnumerateArray())
        {
            if (content.TryGetProperty("attachment", out var attachment))
            {
                if (attachment.TryGetProperty("title", out var title))
                {
                    return title.GetString();
                }
            }
        }
        return null;
    }
}
