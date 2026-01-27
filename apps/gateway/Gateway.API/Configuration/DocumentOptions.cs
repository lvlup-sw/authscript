namespace Gateway.API.Configuration;

/// <summary>
/// Configuration options for document operations.
/// </summary>
public sealed class DocumentOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Document";

    /// <summary>
    /// LOINC code for prior authorization documents.
    /// </summary>
    public string PriorAuthLoincCode { get; init; } = "64289-6";

    /// <summary>
    /// Display name for prior authorization LOINC code.
    /// </summary>
    public string PriorAuthLoincDisplay { get; init; } = "Prior authorization request";

    /// <summary>
    /// Validates the options configuration.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid() => !string.IsNullOrWhiteSpace(PriorAuthLoincCode);
}
