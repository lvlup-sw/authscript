// =============================================================================
// <copyright file="RegisterPatientResponse.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Models;

/// <summary>
/// Response body for the patient registration API endpoint.
/// </summary>
public sealed record RegisterPatientResponse
{
    /// <summary>
    /// Gets the ID of the created work item.
    /// </summary>
    public required string WorkItemId { get; init; }

    /// <summary>
    /// Gets the success message.
    /// </summary>
    public required string Message { get; init; }
}
