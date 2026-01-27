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
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.ReadAsync("Patient", patientId, accessToken, cancellationToken);

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
        var result = await _httpClient.SearchAsync(
            "Condition",
            $"patient={patientId}&clinical-status=active",
            accessToken,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search conditions for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return results;
        }

        var json = result.Value!;
        if (!json.TryGetProperty("entry", out var entries)) return results;
        
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource)) continue;
            
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
        var result = await _httpClient.SearchAsync(
            "Observation",
            $"patient={patientId}&category=laboratory&date=ge{since:yyyy-MM-dd}",
            accessToken,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search observations for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return results;
        }

        var json = result.Value!;
        if (!json.TryGetProperty("entry", out var entries)) return results;
        
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource)) continue;
            
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
        var result = await _httpClient.SearchAsync(
            "Procedure",
            $"patient={patientId}&date=ge{since:yyyy-MM-dd}",
            accessToken,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search procedures for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return results;
        }

        var json = result.Value!;
        if (!json.TryGetProperty("entry", out var entries)) return results;
        
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource)) continue;
            
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

        return results;
    }

    /// <inheritdoc />
    public async Task<List<DocumentInfo>> SearchDocumentsAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DocumentInfo>();
        var result = await _httpClient.SearchAsync(
            "DocumentReference",
            $"patient={patientId}&status=current",
            accessToken,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search documents for {PatientId}: {Error}",
                patientId,
                result.Error?.Message);
            return results;
        }

        var json = result.Value!;
        if (!json.TryGetProperty("entry", out var entries)) return results;
        
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource)) continue;
            
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

        return results;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetDocumentContentAsync(
        string documentId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.ReadBinaryAsync(documentId, accessToken, cancellationToken);

        if (!result.IsFailure) return result.Value;
        
        _logger.LogWarning(
            "Failed to fetch document content {DocumentId}: {Error}",
            documentId,
            result.Error?.Message);
        
        return null;

    }

    private static string? ExtractName(JsonElement json, string part)
    {
        if (!json.TryGetProperty("name", out var names)) return null;

        foreach (var name in names.EnumerateArray())
        {
            switch (part)
            {
                case "given" when name.TryGetProperty("given", out var given):
                {
                    var givenNames = given.EnumerateArray().Select(g => g.GetString() ?? "").ToList();
                    return string.Join(" ", givenNames);
                }
                case "family" when name.TryGetProperty("family", out var family):
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
}
