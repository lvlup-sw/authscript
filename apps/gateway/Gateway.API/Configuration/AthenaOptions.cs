namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for athenahealth FHIR API connectivity and polling.
/// </summary>
public sealed class AthenaOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Athena";

    /// <summary>
    /// Base URL for athenahealth FHIR R4 API.
    /// </summary>
    public required string FhirBaseUrl { get; init; }

    /// <summary>
    /// OAuth client ID for athenahealth.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// OAuth client secret (from user-secrets in dev).
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Token endpoint for OAuth 2.0 client credentials flow.
    /// </summary>
    public required string TokenEndpoint { get; init; }

    /// <summary>
    /// Polling interval for encounter detection, in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// Practice ID for athenahealth API requests.
    /// </summary>
    public string? PracticeId { get; init; }

    /// <summary>
    /// OAuth scopes to request. Space-separated list of FHIR system scopes.
    /// Uses SMART v2 syntax for 2-legged OAuth with system/ prefix.
    /// </summary>
    public string Scopes { get; init; } = string.Join(" ", [
        // Condition scopes (with category filters)
        "system/Condition.r",
        "system/Condition.rs",
        "system/Condition.s",
        "system/Condition.r?category=http://hl7.org/fhir/us/core/CodeSystem/condition-category|health-concern",
        "system/Condition.r?category=http://terminology.hl7.org/CodeSystem/condition-category|encounter-diagnosis",
        "system/Condition.r?category=http://terminology.hl7.org/CodeSystem/condition-category|problem-list-item",
        "system/Condition.rs?category=http://hl7.org/fhir/us/core/CodeSystem/condition-category|health-concern",
        "system/Condition.rs?category=http://terminology.hl7.org/CodeSystem/condition-category|encounter-diagnosis",
        "system/Condition.rs?category=http://terminology.hl7.org/CodeSystem/condition-category|problem-list-item",
        "system/Condition.s?category=http://hl7.org/fhir/us/core/CodeSystem/condition-category|health-concern",
        "system/Condition.s?category=http://terminology.hl7.org/CodeSystem/condition-category|encounter-diagnosis",
        "system/Condition.s?category=http://terminology.hl7.org/CodeSystem/condition-category|problem-list-item",
        // Coverage scopes
        "system/Coverage.r",
        "system/Coverage.rs",
        "system/Coverage.s",
        // DocumentReference scopes
        "system/DocumentReference.r",
        "system/DocumentReference.rs",
        "system/DocumentReference.s",
        // Encounter scopes
        "system/Encounter.r",
        "system/Encounter.rs",
        "system/Encounter.s",
        // Medication scopes
        "system/Medication.r",
        "system/Medication.rs",
        "system/Medication.s",
        // MedicationDispense scopes
        "system/MedicationDispense.r",
        "system/MedicationDispense.rs",
        "system/MedicationDispense.s",
        // MedicationRequest scopes
        "system/MedicationRequest.r",
        "system/MedicationRequest.rs",
        "system/MedicationRequest.s",
        // Observation scopes (with category filters)
        "system/Observation.r",
        "system/Observation.rs",
        "system/Observation.s",
        "system/Observation.r?category=http://hl7.org/fhir/us/core/CodeSystem/us-core-category|sdoh",
        "system/Observation.r?category=http://terminology.hl7.org/CodeSystem/observation-category|laboratory",
        "system/Observation.r?category=http://terminology.hl7.org/CodeSystem/observation-category|procedure",
        "system/Observation.r?category=http://terminology.hl7.org/CodeSystem/observation-category|social-history",
        "system/Observation.r?category=http://terminology.hl7.org/CodeSystem/observation-category|survey",
        "system/Observation.r?category=http://terminology.hl7.org/CodeSystem/observation-category|vital-signs",
        "system/Observation.rs?category=http://hl7.org/fhir/us/core/CodeSystem/us-core-category|sdoh",
        "system/Observation.rs?category=http://terminology.hl7.org/CodeSystem/observation-category|laboratory",
        "system/Observation.rs?category=http://terminology.hl7.org/CodeSystem/observation-category|procedure",
        "system/Observation.rs?category=http://terminology.hl7.org/CodeSystem/observation-category|social-history",
        "system/Observation.rs?category=http://terminology.hl7.org/CodeSystem/observation-category|survey",
        "system/Observation.rs?category=http://terminology.hl7.org/CodeSystem/observation-category|vital-signs",
        "system/Observation.s?category=http://hl7.org/fhir/us/core/CodeSystem/us-core-category|sdoh",
        "system/Observation.s?category=http://terminology.hl7.org/CodeSystem/observation-category|laboratory",
        "system/Observation.s?category=http://terminology.hl7.org/CodeSystem/observation-category|procedure",
        "system/Observation.s?category=http://terminology.hl7.org/CodeSystem/observation-category|social-history",
        "system/Observation.s?category=http://terminology.hl7.org/CodeSystem/observation-category|survey",
        "system/Observation.s?category=http://terminology.hl7.org/CodeSystem/observation-category|vital-signs",
        // Patient scopes
        "system/Patient.r",
        "system/Patient.rs",
        "system/Patient.s",
        // Practitioner scopes
        "system/Practitioner.r",
        "system/Practitioner.rs",
        "system/Practitioner.s",
        // Procedure scopes
        "system/Procedure.r",
        "system/Procedure.rs",
        "system/Procedure.s",
        // ServiceRequest scopes
        "system/ServiceRequest.r",
        "system/ServiceRequest.rs",
        "system/ServiceRequest.s"
    ]);

    /// <summary>
    /// OAuth access token (deprecated - use AthenaTokenStrategy instead).
    /// Kept for backward compatibility during integration.
    /// </summary>
    [Obsolete("Use AthenaTokenStrategy for token acquisition instead.")]
    public string? AccessToken { get; init; }

    /// <summary>
    /// Validates that all required configuration properties are present.
    /// </summary>
    /// <returns>True if ClientId, FhirBaseUrl, and TokenEndpoint are non-empty; otherwise false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(FhirBaseUrl)
            && !string.IsNullOrWhiteSpace(TokenEndpoint);
    }
}
