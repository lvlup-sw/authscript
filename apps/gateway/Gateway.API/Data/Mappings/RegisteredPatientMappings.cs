// =============================================================================
// <copyright file="RegisteredPatientMappings.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data.Mappings;

using Gateway.API.Data.Entities;
using Gateway.API.Models;

/// <summary>
/// Extension methods for mapping between <see cref="RegisteredPatientEntity"/> and <see cref="RegisteredPatient"/>.
/// </summary>
public static class RegisteredPatientMappings
{
    /// <summary>
    /// Converts a <see cref="RegisteredPatientEntity"/> to a <see cref="RegisteredPatient"/> model.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>The corresponding domain model.</returns>
    public static RegisteredPatient ToModel(this RegisteredPatientEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RegisteredPatient
        {
            PatientId = entity.PatientId,
            EncounterId = entity.EncounterId,
            PracticeId = entity.PracticeId,
            WorkItemId = entity.WorkItemId,
            RegisteredAt = entity.RegisteredAt,
            LastPolledAt = entity.LastPolledAt,
            CurrentEncounterStatus = entity.CurrentEncounterStatus
        };
    }

    /// <summary>
    /// Converts a <see cref="RegisteredPatient"/> model to a <see cref="RegisteredPatientEntity"/>.
    /// </summary>
    /// <param name="model">The model to convert.</param>
    /// <returns>The corresponding entity.</returns>
    public static RegisteredPatientEntity ToEntity(this RegisteredPatient model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new RegisteredPatientEntity
        {
            PatientId = model.PatientId,
            EncounterId = model.EncounterId,
            PracticeId = model.PracticeId,
            WorkItemId = model.WorkItemId,
            RegisteredAt = model.RegisteredAt,
            LastPolledAt = model.LastPolledAt,
            CurrentEncounterStatus = model.CurrentEncounterStatus
        };
    }
}
