namespace Gateway.API.Models;

/// <summary>
/// Patient demographic information extracted from FHIR Patient resource.
/// </summary>
public sealed record PatientInfo
{
    /// <summary>
    /// Gets the FHIR resource ID of the patient.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the patient's given (first) name.
    /// </summary>
    public string? GivenName { get; init; }

    /// <summary>
    /// Gets the patient's family (last) name.
    /// </summary>
    public string? FamilyName { get; init; }

    /// <summary>
    /// Gets the patient's date of birth.
    /// </summary>
    public DateOnly? BirthDate { get; init; }

    /// <summary>
    /// Gets the patient's administrative gender.
    /// </summary>
    public string? Gender { get; init; }

    /// <summary>
    /// Gets the patient's insurance member ID.
    /// </summary>
    public string? MemberId { get; init; }

    /// <summary>
    /// Gets the patient's full name (given + family).
    /// </summary>
    public string FullName => $"{GivenName} {FamilyName}".Trim();
}
