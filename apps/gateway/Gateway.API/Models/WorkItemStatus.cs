namespace Gateway.API.Models;

/// <summary>
/// Represents the lifecycle states of a prior authorization work item.
/// </summary>
public enum WorkItemStatus
{
    /// <summary>
    /// All required fields are populated and the work item is awaiting user approval.
    /// </summary>
    ReadyForReview = 0,

    /// <summary>
    /// Required fields are incomplete and the work item needs user action.
    /// </summary>
    MissingData,

    /// <summary>
    /// User marked the work item as unsubmittable. This is a terminal state.
    /// </summary>
    PayerRequirementsNotMet,

    /// <summary>
    /// PDF has been written to the chart and is awaiting manual fax submission.
    /// </summary>
    Submitted,

    /// <summary>
    /// AI determined no prior authorization is needed for this CPT/payer combination.
    /// This is an auto-closed state.
    /// </summary>
    NoPaRequired
}
