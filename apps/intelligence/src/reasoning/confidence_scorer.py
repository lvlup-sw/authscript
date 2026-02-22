"""Weighted LCD compliance confidence scoring algorithm."""

from dataclasses import dataclass
from typing import Literal

from src.models.pa_form import EvidenceItem
from src.models.policy import PolicyDefinition


STATUS_SCORES = {"MET": 1.0, "UNCLEAR": 0.5, "NOT_MET": 0.0}

SCORE_FLOOR = 0.05
GATE_BASE = 0.65
GATE_PENALTY_PER = 0.15


@dataclass
class ScoreResult:
    score: float
    recommendation: Literal["APPROVE", "MANUAL_REVIEW", "NEED_INFO"]


def calculate_confidence(
    evidence: list[EvidenceItem],
    policy: PolicyDefinition,
) -> ScoreResult:
    """Calculate weighted confidence score from evidence and policy."""
    criteria_by_id = {c.id: c for c in policy.criteria}

    # Build bypass set: IDs that are bypassed by a MET criterion
    bypassed_ids: set[str] = set()
    for e in evidence:
        criterion = criteria_by_id.get(e.criterion_id)
        if criterion and e.status == "MET" and criterion.bypasses:
            bypassed_ids.update(criterion.bypasses)

    # Calculate weighted score
    numerator = 0.0
    denominator = 0.0

    for e in evidence:
        criterion = criteria_by_id.get(e.criterion_id)
        if criterion is None:
            continue

        weight = criterion.weight
        llm_conf = e.confidence

        # If bypassed, treat as MET
        if e.criterion_id in bypassed_ids:
            status_score = 1.0
        else:
            status_score = STATUS_SCORES.get(e.status, 0.5)

        numerator += weight * status_score * llm_conf
        denominator += weight

    if denominator == 0:
        raw_score = SCORE_FLOOR
    else:
        raw_score = numerator / denominator

    # Hard gates: required criteria that are NOT_MET (and not bypassed)
    required_not_met = []
    for e in evidence:
        criterion = criteria_by_id.get(e.criterion_id)
        if (
            criterion
            and criterion.required
            and e.status == "NOT_MET"
            and e.criterion_id not in bypassed_ids
        ):
            required_not_met.append(e)

    if required_not_met:
        gate_cap = GATE_BASE - GATE_PENALTY_PER * len(required_not_met)
        raw_score = min(raw_score, gate_cap)

    # Floor and ceiling
    final_score = max(SCORE_FLOOR, min(1.0, raw_score))

    # Recommendation from score
    if final_score >= 0.80:
        recommendation = "APPROVE"
    elif final_score >= 0.50:
        recommendation = "MANUAL_REVIEW"
    else:
        recommendation = "NEED_INFO"

    return ScoreResult(score=round(final_score, 4), recommendation=recommendation)
