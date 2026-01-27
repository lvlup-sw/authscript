namespace Gateway.API.Services.Fhir;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Gateway.API.Contracts.Fhir;
using Microsoft.Extensions.Logging;

/// <summary>
/// FHIR JSON serialization using Hl7.Fhir library.
/// </summary>
public sealed class FhirSerializer : IFhirSerializer
{
    private static readonly FhirJsonSerializer s_serializer = new();
    private static readonly FhirJsonParser s_parser = new();
    private readonly ILogger<FhirSerializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirSerializer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirSerializer(ILogger<FhirSerializer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Serialize<T>(T resource) where T : Resource
    {
        ArgumentNullException.ThrowIfNull(resource);
        try
        {
            return s_serializer.SerializeToString(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize {ResourceType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string json) where T : Resource
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return s_parser.Parse<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize {ResourceType}", typeof(T).Name);
            return null;
        }
    }

    /// <inheritdoc />
    public Bundle? DeserializeBundle(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return s_parser.Parse<Bundle>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Bundle");
            return null;
        }
    }
}
