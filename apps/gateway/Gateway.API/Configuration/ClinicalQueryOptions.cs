namespace Gateway.API.Configuration;

/// <summary>
/// Configuration options for clinical FHIR queries.
/// </summary>
public sealed class ClinicalQueryOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ClinicalQuery";

    /// <summary>
    /// Gets or sets the lookback period in months for observations.
    /// </summary>
    public int ObservationLookbackMonths { get; init; } = 6;

    /// <summary>
    /// Gets or sets the lookback period in months for procedures.
    /// </summary>
    public int ProcedureLookbackMonths { get; init; } = 12;

    /// <summary>
    /// Validates the options configuration.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid() => ObservationLookbackMonths > 0 && ProcedureLookbackMonths > 0;
}
