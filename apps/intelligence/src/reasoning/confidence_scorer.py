"""Confidence scoring algorithm for evidence against policy criteria."""

from dataclasses import dataclass
from typing import Literal

from src.models.pa_form import EvidenceItem
from src.models.policy import PolicyDefinition


@dataclass
class ScoreResult:
    """Result of confidence scoring."""

    score: float
    recommendation: Literal["APPROVE", "NEED_INFO", "MANUAL_REVIEW"]


def calculate_confidence(
    evidence: list[EvidenceItem],
    policy: PolicyDefinition,
) -> ScoreResult:
    """Calculate weighted confidence score and recommendation from evidence.

    Uses policy criterion weights to compute a weighted average of evidence
    confidence scores for MET criteria. Criteria that are NOT_MET or UNCLEAR
    reduce confidence.

    Args:
        evidence: List of evidence items from extraction
        policy: Policy definition with weighted criteria

    Returns:
        ScoreResult with weighted score and recommendation
    """
    if not evidence or not policy.criteria:
        return ScoreResult(score=0.5, recommendation="MANUAL_REVIEW")

    # Build lookup from criterion_id -> weight
    weight_map: dict[str, float] = {}
    required_ids: set[str] = set()
    for criterion in policy.criteria:
        weight_map[criterion.id] = criterion.weight
        if criterion.required:
            required_ids.add(criterion.id)

    total_weight = 0.0
    weighted_score = 0.0
    has_not_met = False
    has_unclear = False

    for item in evidence:
        w = weight_map.get(item.criterion_id, 1.0)
        total_weight += w

        if item.status == "MET":
            weighted_score += w * item.confidence
        elif item.status == "NOT_MET":
            has_not_met = True
            # NOT_MET contributes 0 to weighted score
        else:  # UNCLEAR
            has_unclear = True
            weighted_score += w * item.confidence * 0.5  # Partial credit

    if total_weight == 0:
        return ScoreResult(score=0.5, recommendation="MANUAL_REVIEW")

    score = weighted_score / total_weight

    # Check if required criteria are met
    required_met = all(
        item.status == "MET"
        for item in evidence
        if item.criterion_id in required_ids
    )

    # Determine recommendation
    if required_met and not has_not_met and not has_unclear and score >= 0.7:
        recommendation: Literal["APPROVE", "NEED_INFO", "MANUAL_REVIEW"] = "APPROVE"
    elif has_not_met:
        recommendation = "NEED_INFO"
    else:
        recommendation = "MANUAL_REVIEW"

    return ScoreResult(score=round(score, 2), recommendation=recommendation)
