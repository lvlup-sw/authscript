// =============================================================================
// <copyright file="PostgresPARequestStore.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Services;

using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Data;
using Gateway.API.Data.Mappings;
using Gateway.API.GraphQL.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// PostgreSQL-backed implementation of IPARequestStore using EF Core.
/// </summary>
public sealed class PostgresPARequestStore : IPARequestStore
{
    private static readonly SemaphoreSlim IdGenerationLock = new(1, 1);

    private readonly GatewayDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresPARequestStore"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PostgresPARequestStore(GatewayDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<PARequestModel> CreateAsync(PARequestModel request, string fhirPatientId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirPatientId);

        var id = await GenerateIdAsync(ct).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        var model = request with
        {
            Id = id,
            CreatedAt = now.ToString("o"),
            UpdatedAt = now.ToString("o"),
        };

        var entity = model.ToEntity(fhirPatientId);
        _context.PriorAuthRequests.Add(entity);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return model;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PARequestModel>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _context.PriorAuthRequests
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(e => e.ToModel()).ToList();
    }

    /// <inheritdoc/>
    public async Task<PARequestModel?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.PriorAuthRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        return entity?.ToModel();
    }

    /// <inheritdoc/>
    public async Task<PARequestModel?> UpdateFieldsAsync(
        string id,
        string? diagnosis,
        string? diagnosisCode,
        string? serviceDate,
        string? placeOfService,
        string? clinicalSummary,
        IReadOnlyList<CriterionModel>? criteria,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.PriorAuthRequests
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        if (diagnosis is not null)
        {
            entity.DiagnosisName = diagnosis;
        }

        if (diagnosisCode is not null)
        {
            entity.DiagnosisCode = diagnosisCode;
        }

        if (serviceDate is not null)
        {
            entity.ServiceDate = serviceDate;
        }

        if (placeOfService is not null)
        {
            entity.PlaceOfService = placeOfService;
        }

        if (clinicalSummary is not null)
        {
            entity.ClinicalSummary = clinicalSummary;
        }

        if (criteria is not null)
        {
            entity.CriteriaJson = JsonSerializer.Serialize(criteria, PriorAuthRequestMappings.JsonOptions);
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    /// <inheritdoc/>
    public async Task<PARequestModel?> ApplyAnalysisResultAsync(
        string id,
        string clinicalSummary,
        int confidence,
        IReadOnlyList<CriterionModel> criteria,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.PriorAuthRequests
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        entity.Status = "ready";
        entity.ClinicalSummary = clinicalSummary;
        entity.Confidence = confidence;
        entity.CriteriaJson = JsonSerializer.Serialize(criteria, PriorAuthRequestMappings.JsonOptions);
        entity.ReadyAt = now;
        entity.UpdatedAt = now;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    /// <inheritdoc/>
    public async Task<PARequestModel?> SubmitAsync(string id, int addReviewTimeSeconds = 0, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.PriorAuthRequests
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        entity.Status = "waiting_for_insurance";
        entity.SubmittedAt = now;
        entity.ReviewTimeSeconds += addReviewTimeSeconds;
        entity.UpdatedAt = now;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    /// <inheritdoc/>
    public async Task<PARequestModel?> AddReviewTimeAsync(string id, int seconds, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.PriorAuthRequests
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        entity.ReviewTimeSeconds += seconds;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.ToModel();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _context.PriorAuthRequests
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        _context.PriorAuthRequests.Remove(entity);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc/>
    public async Task<PAStatsModel> GetStatsAsync(CancellationToken ct = default)
    {
        var counts = await _context.PriorAuthRequests
            .AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var lookup = counts.ToDictionary(c => c.Status, c => c.Count);

        var ready = lookup.GetValueOrDefault("ready");
        var waitingForInsurance = lookup.GetValueOrDefault("waiting_for_insurance");
        var approved = lookup.GetValueOrDefault("approved");
        var denied = lookup.GetValueOrDefault("denied");
        var draft = lookup.GetValueOrDefault("draft");

        return new PAStatsModel
        {
            Ready = ready,
            Submitted = approved + denied,
            WaitingForInsurance = waitingForInsurance,
            Attention = draft,
            Total = counts.Sum(c => c.Count),
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActivityItemModel>> GetActivityAsync(CancellationToken ct = default)
    {
        var entities = await _context.PriorAuthRequests
            .AsNoTracking()
            .OrderByDescending(e => e.UpdatedAt)
            .Take(5)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(e => new ActivityItemModel
        {
            Id = e.Id,
            Action = e.Status switch
            {
                "approved" or "denied" or "waiting_for_insurance" => "PA submitted",
                "ready" => "Ready for review",
                _ => "Updated",
            },
            PatientName = e.PatientName,
            ProcedureCode = e.ProcedureCode,
            Time = ToRelativeTimeAgo(e.UpdatedAt),
            Type = e.Status switch
            {
                "approved" or "denied" or "waiting_for_insurance" => "success",
                "ready" => "ready",
                _ => "info",
            },
        }).ToList();
    }

    private async Task<string> GenerateIdAsync(CancellationToken ct)
    {
        await IdGenerationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var maxId = await _context.PriorAuthRequests
                .AsNoTracking()
                .Select(e => e.Id)
                .OrderByDescending(id => id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var counter = 1;
            if (maxId is not null && maxId.StartsWith("PA-") && int.TryParse(maxId[3..], out var existing))
            {
                counter = existing + 1;
            }

            return $"PA-{counter:D3}";
        }
        finally
        {
            IdGenerationLock.Release();
        }
    }

    private static string ToRelativeTimeAgo(DateTimeOffset updatedAt)
    {
        var ago = DateTimeOffset.UtcNow - updatedAt;
        if (ago.TotalSeconds < 60)
        {
            return $"{(int)ago.TotalSeconds}s ago";
        }

        if (ago.TotalMinutes < 60)
        {
            return $"{(int)ago.TotalMinutes}m ago";
        }

        if (ago.TotalHours < 24)
        {
            return $"{(int)ago.TotalHours}h ago";
        }

        if (ago.TotalDays < 30)
        {
            return $"{(int)ago.TotalDays}d ago";
        }

        return $"{(int)ago.TotalDays / 30}mo ago";
    }
}
