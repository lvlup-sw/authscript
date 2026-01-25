using System.Text.Json.Serialization;

namespace Gateway.API.Models;

/// <summary>
/// Prior Authorization form data returned from the Intelligence service.
/// Contains patient info, clinical summary, and field mappings for PDF stamping.
/// </summary>
public sealed record PAFormData
{
    /// <summary>
    /// Gets the patient's full name.
    /// </summary>
    [JsonPropertyName("patient_name")]
    public required string PatientName { get; init; }

    /// <summary>
    /// Gets the patient's date of birth as a formatted string.
    /// </summary>
    [JsonPropertyName("patient_dob")]
    public required string PatientDob { get; init; }

    /// <summary>
    /// Gets the patient's insurance member ID.
    /// </summary>
    [JsonPropertyName("member_id")]
    public required string MemberId { get; init; }

    /// <summary>
    /// Gets the list of diagnosis codes (ICD-10) for the authorization.
    /// </summary>
    [JsonPropertyName("diagnosis_codes")]
    public required List<string> DiagnosisCodes { get; init; }

    /// <summary>
    /// Gets the procedure code (CPT) being requested.
    /// </summary>
    [JsonPropertyName("procedure_code")]
    public required string ProcedureCode { get; init; }

    /// <summary>
    /// Gets the AI-generated clinical summary supporting the request.
    /// </summary>
    [JsonPropertyName("clinical_summary")]
    public required string ClinicalSummary { get; init; }

    /// <summary>
    /// Gets the list of evidence items supporting each criterion.
    /// </summary>
    [JsonPropertyName("supporting_evidence")]
    public required List<EvidenceItem> SupportingEvidence { get; init; }

    /// <summary>
    /// Gets the AI recommendation: "approve", "deny", or "review".
    /// </summary>
    [JsonPropertyName("recommendation")]
    public required string Recommendation { get; init; }

    /// <summary>
    /// Gets the overall confidence score for the recommendation (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence_score")]
    public required double ConfidenceScore { get; init; }

    /// <summary>
    /// Gets the mapping of PDF form field names to values for stamping.
    /// </summary>
    [JsonPropertyName("field_mappings")]
    public required Dictionary<string, string> FieldMappings { get; init; }
}
