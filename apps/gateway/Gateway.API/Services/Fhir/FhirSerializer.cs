namespace Gateway.API.Services.Fhir;

using Gateway.API.Contracts.Fhir;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

/// <summary>
/// Serializes and deserializes FHIR resources using Firely SDK.
/// </summary>
public sealed class FhirSerializer : IFhirSerializer
{
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirSerializer"/> class.
    /// </summary>
    public FhirSerializer()
    {
        _serializer = new FhirJsonSerializer(new SerializerSettings { Pretty = false });
        _parser = new FhirJsonParser(new ParserSettings { PermissiveParsing = true });
    }

    /// <inheritdoc />
    public string Serialize<T>(T resource) where T : Base
    {
        return _serializer.SerializeToString(resource);
    }

    /// <inheritdoc />
    public T Deserialize<T>(string json) where T : Base
    {
        return _parser.Parse<T>(json);
    }
}
