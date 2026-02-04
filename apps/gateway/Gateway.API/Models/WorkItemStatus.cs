// =============================================================================
// <copyright file="WorkItemStatus.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Models;

/// <summary>
/// Represents the lifecycle states of a prior authorization work item.
/// </summary>
public enum WorkItemStatus
{
    /// <summary>
    /// Patient registered, awaiting encounter completion.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// All required fields are populated and the work item is awaiting user approval.
    /// </summary>
    ReadyForReview = 1,

    /// <summary>
    /// Required fields are incomplete and the work item needs user action.
    /// </summary>
    MissingData = 2,

    /// <summary>
    /// User marked the work item as unsubmittable. This is a terminal state.
    /// </summary>
    PayerRequirementsNotMet = 3,

    /// <summary>
    /// PDF has been written to the chart and is awaiting manual fax submission.
    /// </summary>
    Submitted = 4,

    /// <summary>
    /// AI determined no prior authorization is needed for this CPT/payer combination.
    /// This is an auto-closed state.
    /// </summary>
    NoPaRequired = 5,
}
