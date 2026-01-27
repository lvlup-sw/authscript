namespace Gateway.API.Contracts.Fhir;

using Hl7.Fhir.Model;

/// <summary>
/// Serializes and deserializes FHIR resources.
/// </summary>
public interface IFhirSerializer
{
    /// <summary>
    /// Serializes a FHIR resource to JSON.
    /// </summary>
    /// <typeparam name="T">The FHIR resource type.</typeparam>
    /// <param name="resource">The resource to serialize.</param>
    /// <returns>JSON string representation.</returns>
    string Serialize<T>(T resource) where T : Base;

    /// <summary>
    /// Deserializes JSON to a FHIR resource.
    /// </summary>
    /// <typeparam name="T">The FHIR resource type.</typeparam>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized resource.</returns>
    T Deserialize<T>(string json) where T : Base;
}
