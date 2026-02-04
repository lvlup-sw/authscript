// =============================================================================
// <copyright file="WorkItemEntity.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Entities;

using Gateway.API.Models;

/// <summary>
/// Entity Framework Core entity representing a prior authorization work item in the database.
/// </summary>
public sealed class WorkItemEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the work item.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the FHIR Patient ID associated with this work item.
    /// </summary>
    public required string PatientId { get; set; }

    /// <summary>
    /// Gets or sets the FHIR Encounter ID that triggered this work item.
    /// </summary>
    public required string EncounterId { get; set; }

    /// <summary>
    /// Gets or sets the FHIR ServiceRequest ID for the order requiring prior authorization.
    /// Null at registration time; populated after analysis.
    /// </summary>
    public string? ServiceRequestId { get; set; }

    /// <summary>
    /// Gets or sets the CPT code for the procedure requiring prior authorization.
    /// Null at registration time; populated after analysis.
    /// </summary>
    public string? ProcedureCode { get; set; }

    /// <summary>
    /// Gets or sets the current status of the work item in its lifecycle.
    /// </summary>
    public WorkItemStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the work item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the work item was last updated.
    /// Null if the work item has never been updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
