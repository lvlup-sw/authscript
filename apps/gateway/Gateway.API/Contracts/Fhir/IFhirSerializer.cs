namespace Gateway.API.Contracts.Fhir;

using Hl7.Fhir.Model;

/// <summary>
/// Abstraction for FHIR JSON serialization.
/// </summary>
public interface IFhirSerializer
{
    /// <summary>Serialize a FHIR resource to JSON string.</summary>
    string Serialize<T>(T resource) where T : Resource;

    /// <summary>Deserialize JSON string to FHIR resource.</summary>
    T? Deserialize<T>(string json) where T : Resource;

    /// <summary>Deserialize JSON to a Bundle resource.</summary>
    Bundle? DeserializeBundle(string json);
}
