// =============================================================================
// <copyright file="RecommendationMapper.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Maps analysis recommendations to work item statuses with normalized case handling.
/// </summary>
public static class RecommendationMapper
{
    /// <summary>
    /// Maps a recommendation string to the appropriate work item status.
    /// </summary>
    /// <param name="recommendation">The recommendation from the analysis service.</param>
    /// <param name="confidenceScore">Optional confidence score for conditional mapping.</param>
    /// <returns>The appropriate work item status.</returns>
    public static WorkItemStatus MapToStatus(string? recommendation, double confidenceScore = 1.0)
    {
        var normalized = recommendation?.ToUpperInvariant();

        return normalized switch
        {
            "APPROVE" when confidenceScore >= 0.8 => WorkItemStatus.ReadyForReview,
            "APPROVE" => WorkItemStatus.MissingData,
            "DENY" => WorkItemStatus.ReadyForReview,
            "NEEDS_INFO" => WorkItemStatus.MissingData,
            "NOT_REQUIRED" or "NO_PA_REQUIRED" => WorkItemStatus.NoPaRequired,
            _ => WorkItemStatus.ReadyForReview
        };
    }
}
