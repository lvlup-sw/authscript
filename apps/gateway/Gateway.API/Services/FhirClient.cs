using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// High-level FHIR client implementation.
/// Delegates HTTP operations to IFhirHttpClient and maps responses to domain DTOs.
/// </summary>
public sealed class FhirClient : IFhirClient
{
    private readonly IFhirHttpClient _httpClient;
    private readonly ILogger<FhirClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirClient"/> class.
    /// </summary>
    /// <param name="httpClient">Low-level FHIR HTTP client.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirClient(IFhirHttpClient httpClient, ILogger<FhirClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PatientInfo?> GetPatientAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.ReadAsync("Patient", patientId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to fetch patient {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return null;
        }

        var json = result.Value!;
        return new PatientInfo
        {
            Id = patientId,
            GivenName = ExtractGivenName(json),
            FamilyName = ExtractFamilyName(json),
            BirthDate = ExtractDate(json, "birthDate"),
            Gender = json.TryGetProperty("gender", out var gender) ? gender.GetString() : null
        };
    }

    /// <inheritdoc />
    public async Task<List<ConditionInfo>> SearchConditionsAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.SearchAsync(
            "Condition",
            $"patient={patientId}&clinical-status=active",
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search conditions for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return [];
        }

        return ExtractResourcesFromBundle(result.Value!, MapCondition);
    }

    /// <inheritdoc />
    public async Task<List<ObservationInfo>> SearchObservationsAsync(
        string patientId,
        DateOnly since,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.SearchAsync(
            "Observation",
            $"patient={patientId}&category=laboratory&date=ge{since:yyyy-MM-dd}",
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search observations for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return [];
        }

        return ExtractResourcesFromBundle(result.Value!, MapObservation);
    }

    /// <inheritdoc />
    public async Task<List<ProcedureInfo>> SearchProceduresAsync(
        string patientId,
        DateOnly since,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.SearchAsync(
            "Procedure",
            $"patient={patientId}&date=ge{since:yyyy-MM-dd}",
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search procedures for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return [];
        }

        return ExtractResourcesFromBundle(result.Value!, MapProcedure);
    }

    /// <inheritdoc />
    public async Task<List<DocumentInfo>> SearchDocumentsAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.SearchAsync(
            "DocumentReference",
            $"patient={patientId}&status=current",
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search documents for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return [];
        }

        return ExtractResourcesFromBundle(result.Value!, MapDocument);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetDocumentContentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.ReadBinaryAsync(documentId, cancellationToken);

        if (!result.IsFailure) return result.Value;

        _logger.LogWarning(
            "Failed to fetch document content {DocumentId}: {Error}",
            documentId,
            result.Error?.Message);

        return null;
    }

    /// <inheritdoc />
    public async Task<List<ServiceRequestInfo>> SearchServiceRequestsAsync(
        string patientId,
        string? encounterId,
        CancellationToken cancellationToken = default)
    {
        var query = $"patient={patientId}";
        if (!string.IsNullOrEmpty(encounterId))
        {
            query += $"&encounter={encounterId}";
        }

        var result = await _httpClient.SearchAsync(
            "ServiceRequest",
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search service requests for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return [];
        }

        return ExtractResourcesFromBundle(result.Value!, MapServiceRequest);
    }

    #region Bundle Extraction

    private static List<T> ExtractResourcesFromBundle<T>(
        JsonElement bundle,
        Func<JsonElement, T?> resourceMapper) where T : class
    {
        var results = new List<T>();

        if (!bundle.TryGetProperty("entry", out var entries)) return results;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource)) continue;

            var mapped = resourceMapper(resource);
            if (mapped is not null)
            {
                results.Add(mapped);
            }
        }

        return results;
    }

    private static ConditionInfo? MapCondition(JsonElement resource)
    {
        var coding = ExtractFirstCoding(resource, "code");
        if (coding is null) return null;

        return new ConditionInfo
        {
            Id = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
            Code = coding.Value.code,
            CodeSystem = coding.Value.system,
            Display = coding.Value.display,
            ClinicalStatus = ExtractClinicalStatus(resource)
        };
    }

    private static ObservationInfo? MapObservation(JsonElement resource)
    {
        var coding = ExtractFirstCoding(resource, "code");
        if (coding is null) return null;

        return new ObservationInfo
        {
            Id = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
            Code = coding.Value.code,
            CodeSystem = coding.Value.system,
            Display = coding.Value.display,
            Value = ExtractObservationValue(resource),
            Unit = ExtractObservationUnit(resource)
        };
    }

    private static ProcedureInfo? MapProcedure(JsonElement resource)
    {
        var coding = ExtractFirstCoding(resource, "code");
        if (coding is null) return null;

        return new ProcedureInfo
        {
            Id = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString(),
            Code = coding.Value.code,
            CodeSystem = coding.Value.system,
            Display = coding.Value.display,
            Status = resource.TryGetProperty("status", out var status) ? status.GetString() : null
        };
    }

    private static DocumentInfo MapDocument(JsonElement resource)
    {
        var docId = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString();
        var type = ExtractFirstCoding(resource, "type");

        return new DocumentInfo
        {
            Id = docId,
            Type = type?.display ?? type?.code ?? "Unknown",
            ContentType = ExtractContentType(resource),
            Title = ExtractDocumentTitle(resource)
        };
    }

    private static ServiceRequestInfo? MapServiceRequest(JsonElement resource)
    {
        var code = ExtractCodeableConcept(resource, "code");
        if (code is null) return null;

        var resourceId = resource.TryGetProperty("id", out var id) ? id.GetString()! : Guid.NewGuid().ToString();
        var rawStatus = resource.TryGetProperty("status", out var s) ? s.GetString() : null;
        var status = string.IsNullOrWhiteSpace(rawStatus) ? "unknown" : rawStatus;

        return new ServiceRequestInfo
        {
            Id = resourceId,
            Status = status,
            Code = code,
            EncounterId = ExtractEncounterId(resource),
            AuthoredOn = ExtractDateTimeOffset(resource, "authoredOn")
        };
    }

    #endregion

    #region Name Extraction

    private static string? ExtractGivenName(JsonElement json)
    {
        if (!json.TryGetProperty("name", out var names)) return null;

        foreach (var name in names.EnumerateArray())
        {
            if (name.TryGetProperty("given", out var given))
            {
                var givenNames = given.EnumerateArray().Select(g => g.GetString() ?? "").ToList();
                return string.Join(" ", givenNames);
            }
        }

        return null;
    }

    private static string? ExtractFamilyName(JsonElement json)
    {
        if (!json.TryGetProperty("name", out var names)) return null;

        foreach (var name in names.EnumerateArray())
        {
            if (name.TryGetProperty("family", out var family))
            {
                return family.GetString();
            }
        }

        return null;
    }

    #endregion

    #region JSON Property Extraction

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
        var coding = ExtractFirstCoding(resource, "clinicalStatus");
        return coding?.code;
    }

    private static string? ExtractObservationValue(JsonElement resource)
    {
        if (resource.TryGetProperty("valueQuantity", out var quantity))
        {
            return quantity.TryGetProperty("value", out var v) ? v.ToString() : null;
        }

        return resource.TryGetProperty("valueString", out var str)
            ? str.GetString()
            : null;
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
            if (!content.TryGetProperty("attachment", out var attachment)) continue;

            if (attachment.TryGetProperty("contentType", out var ct))
            {
                return ct.GetString();
            }
        }
        return null;
    }

    private static string? ExtractDocumentTitle(JsonElement resource)
    {
        if (!resource.TryGetProperty("content", out var contents)) return null;

        foreach (var content in contents.EnumerateArray())
        {
            if (!content.TryGetProperty("attachment", out var attachment)) continue;

            if (attachment.TryGetProperty("title", out var title))
            {
                return title.GetString();
            }
        }
        return null;
    }

    private static CodeableConcept? ExtractCodeableConcept(JsonElement resource, string property)
    {
        if (!resource.TryGetProperty(property, out var codeableConcept)) return null;

        var codings = new List<Coding>();
        if (codeableConcept.TryGetProperty("coding", out var codingsArray))
        {
            foreach (var coding in codingsArray.EnumerateArray())
            {
                codings.Add(new Coding
                {
                    System = coding.TryGetProperty("system", out var sys) ? sys.GetString() : null,
                    Code = coding.TryGetProperty("code", out var code) ? code.GetString() : null,
                    Display = coding.TryGetProperty("display", out var display) ? display.GetString() : null
                });
            }
        }

        return new CodeableConcept
        {
            Coding = codings.Count > 0 ? codings : null,
            Text = codeableConcept.TryGetProperty("text", out var text) ? text.GetString() : null
        };
    }

    private static string? ExtractEncounterId(JsonElement resource)
    {
        if (!resource.TryGetProperty("encounter", out var encounter)) return null;
        if (!encounter.TryGetProperty("reference", out var reference)) return null;

        var refStr = reference.GetString();
        if (string.IsNullOrEmpty(refStr)) return null;

        // Parse "Encounter/{id}" format
        const string prefix = "Encounter/";
        if (refStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return refStr[prefix.Length..];
        }

        return null;
    }

    private static DateTimeOffset? ExtractDateTimeOffset(JsonElement resource, string property)
    {
        if (!resource.TryGetProperty(property, out var value)) return null;
        var str = value.GetString();
        if (string.IsNullOrEmpty(str)) return null;

        return DateTimeOffset.TryParse(str, out var result) ? result : null;
    }

    #endregion
}
