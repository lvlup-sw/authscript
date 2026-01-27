namespace Gateway.API.Contracts.Fhir;

using Hl7.Fhir.Model;

/// <summary>
/// Abstraction for FHIR JSON serialization.
/// </summary>
public interface IFhirSerializer
{
    /// <summary>
    /// Serialize a FHIR resource to JSON string.
    /// </summary>
    /// <typeparam name="T">The FHIR resource type.</typeparam>
    /// <param name="resource">The resource to serialize.</param>
    /// <returns>JSON string representation of the resource.</returns>
    string Serialize<T>(T resource) where T : Resource;

    /// <summary>
    /// Deserialize JSON string to FHIR resource.
    /// </summary>
    /// <typeparam name="T">The FHIR resource type.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized resource, or null if deserialization fails.</returns>
    T? Deserialize<T>(string json) where T : Resource;

    /// <summary>
    /// Deserialize JSON to a Bundle resource.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized Bundle, or null if deserialization fails.</returns>
    Bundle? DeserializeBundle(string json);
}
