// =============================================================================
// <copyright file="WorkItemMappings.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Mappings;

using Gateway.API.Data.Entities;
using Gateway.API.Models;

/// <summary>
/// Extension methods for mapping between <see cref="WorkItem"/> and <see cref="WorkItemEntity"/>.
/// </summary>
public static class WorkItemMappings
{
    /// <summary>
    /// Converts a <see cref="WorkItemEntity"/> to a <see cref="WorkItem"/> domain model.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A new <see cref="WorkItem"/> with values copied from the entity.</returns>
    public static WorkItem ToModel(this WorkItemEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new WorkItem
        {
            Id = entity.Id,
            PatientId = entity.PatientId,
            EncounterId = entity.EncounterId,
            ServiceRequestId = entity.ServiceRequestId,
            ProcedureCode = entity.ProcedureCode,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a <see cref="WorkItem"/> domain model to a <see cref="WorkItemEntity"/>.
    /// </summary>
    /// <param name="model">The model to convert.</param>
    /// <returns>A new <see cref="WorkItemEntity"/> with values copied from the model.</returns>
    public static WorkItemEntity ToEntity(this WorkItem model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new WorkItemEntity
        {
            Id = model.Id,
            PatientId = model.PatientId,
            EncounterId = model.EncounterId,
            ServiceRequestId = model.ServiceRequestId,
            ProcedureCode = model.ProcedureCode,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
}
