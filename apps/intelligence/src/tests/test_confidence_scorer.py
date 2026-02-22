"""Tests for weighted LCD compliance confidence scorer."""
import pytest
from src.models.pa_form import EvidenceItem
from src.models.policy import PolicyCriterion, PolicyDefinition
from src.reasoning.confidence_scorer import ScoreResult, calculate_confidence


def _make_criterion(id: str, weight: float, required: bool = False, bypasses: list[str] | None = None) -> PolicyCriterion:
    return PolicyCriterion(id=id, description=f"Test {id}", weight=weight, required=required, bypasses=bypasses or [])

def _make_evidence(criterion_id: str, status: str, confidence: float = 0.9) -> EvidenceItem:
    return EvidenceItem(criterion_id=criterion_id, status=status, evidence="test", source="test", confidence=confidence)

def _make_policy(criteria: list[PolicyCriterion]) -> PolicyDefinition:
    return PolicyDefinition(policy_id="test", policy_name="Test", payer="Test", procedure_codes=["72148"], criteria=criteria)


def test_all_met_high_confidence():
    """All criteria MET with high confidence -> score >= 0.85, APPROVE."""
    criteria = [_make_criterion("c1", 0.3), _make_criterion("c2", 0.3), _make_criterion("c3", 0.4)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "MET", 0.9), _make_evidence("c2", "MET", 0.9), _make_evidence("c3", "MET", 0.9)]
    result = calculate_confidence(evidence, policy)
    assert result.score >= 0.85
    assert result.recommendation == "APPROVE"


def test_all_not_met_hits_floor():
    """All NOT_MET -> score = 0.05 (floor)."""
    criteria = [_make_criterion("c1", 0.5, required=True), _make_criterion("c2", 0.5, required=True)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "NOT_MET", 0.9), _make_evidence("c2", "NOT_MET", 0.9)]
    result = calculate_confidence(evidence, policy)
    assert result.score == pytest.approx(0.05, abs=0.01)


def test_mixed_met_and_optional_not_met():
    """3 MET + 1 optional NOT_MET -> APPROVE (90% weight MET at 0.9 conf)."""
    criteria = [
        _make_criterion("c1", 0.3, required=True),
        _make_criterion("c2", 0.3, required=True),
        _make_criterion("c3", 0.3, required=True),
        _make_criterion("c4", 0.1, required=False),
    ]
    policy = _make_policy(criteria)
    evidence = [
        _make_evidence("c1", "MET"), _make_evidence("c2", "MET"),
        _make_evidence("c3", "MET"), _make_evidence("c4", "NOT_MET"),
    ]
    result = calculate_confidence(evidence, policy)
    assert 0.80 <= result.score <= 0.85


def test_required_not_met_caps_score():
    """4 MET + 1 required NOT_MET -> score capped at 0.50."""
    criteria = [
        _make_criterion("c1", 0.2), _make_criterion("c2", 0.2),
        _make_criterion("c3", 0.2), _make_criterion("c4", 0.2),
        _make_criterion("c5", 0.2, required=True),
    ]
    policy = _make_policy(criteria)
    evidence = [
        _make_evidence("c1", "MET"), _make_evidence("c2", "MET"),
        _make_evidence("c3", "MET"), _make_evidence("c4", "MET"),
        _make_evidence("c5", "NOT_MET"),
    ]
    result = calculate_confidence(evidence, policy)
    assert result.score <= 0.50


def test_multiple_required_not_met_stacks_penalty():
    """2 required NOT_MET -> lower cap than 1."""
    criteria = [
        _make_criterion("c1", 0.3, required=True),
        _make_criterion("c2", 0.3, required=True),
        _make_criterion("c3", 0.4),
    ]
    policy = _make_policy(criteria)
    evidence_one = [_make_evidence("c1", "MET"), _make_evidence("c2", "NOT_MET"), _make_evidence("c3", "MET")]
    evidence_two = [_make_evidence("c1", "NOT_MET"), _make_evidence("c2", "NOT_MET"), _make_evidence("c3", "MET")]
    result_one = calculate_confidence(evidence_one, policy)
    result_two = calculate_confidence(evidence_two, policy)
    assert result_two.score < result_one.score


def test_unclear_contributes_half():
    """All UNCLEAR with medium confidence -> score ~0.35.

    UNCLEAR has status_score=0.5, multiplied by llm_conf=0.7, giving
    numerator = 0.35 against denominator = 1.0.
    """
    criteria = [_make_criterion("c1", 0.5), _make_criterion("c2", 0.5)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "UNCLEAR", 0.7), _make_evidence("c2", "UNCLEAR", 0.7)]
    result = calculate_confidence(evidence, policy)
    assert 0.30 <= result.score <= 0.40


def test_bypass_treats_bypassed_as_met():
    """Criterion with bypasses=['c2'] MET -> c2 treated as MET."""
    criteria = [
        _make_criterion("c1", 0.5, bypasses=["c2"]),
        _make_criterion("c2", 0.5, required=True),
    ]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "MET"), _make_evidence("c2", "NOT_MET")]
    result = calculate_confidence(evidence, policy)
    # c2 should be treated as MET because c1 (which bypasses c2) is MET
    assert result.score >= 0.80


def test_bypass_ignored_when_bypasser_not_met():
    """Bypass criterion NOT_MET -> bypassed criterion evaluated normally."""
    criteria = [
        _make_criterion("c1", 0.5, bypasses=["c2"]),
        _make_criterion("c2", 0.5, required=True),
    ]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "NOT_MET"), _make_evidence("c2", "NOT_MET")]
    result = calculate_confidence(evidence, policy)
    assert result.score <= 0.50


def test_recommendation_approve_threshold():
    """Score >= 0.80 -> APPROVE."""
    criteria = [_make_criterion("c1", 1.0)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "MET", 0.9)]
    result = calculate_confidence(evidence, policy)
    assert result.recommendation == "APPROVE"


def test_recommendation_manual_review_threshold():
    """Score in [0.50, 0.80) -> MANUAL_REVIEW."""
    criteria = [_make_criterion("c1", 0.5), _make_criterion("c2", 0.5)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "MET", 0.9), _make_evidence("c2", "UNCLEAR", 0.7)]
    result = calculate_confidence(evidence, policy)
    assert result.recommendation == "MANUAL_REVIEW"


def test_recommendation_need_info_threshold():
    """Score < 0.50 -> NEED_INFO."""
    criteria = [_make_criterion("c1", 0.5, required=True), _make_criterion("c2", 0.5)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "NOT_MET", 0.9), _make_evidence("c2", "NOT_MET", 0.9)]
    result = calculate_confidence(evidence, policy)
    assert result.recommendation == "NEED_INFO"


def test_score_floor_never_below_five_percent():
    """Extreme inputs -> min 0.05."""
    criteria = [_make_criterion("c1", 1.0, required=True)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "NOT_MET", 0.99)]
    result = calculate_confidence(evidence, policy)
    assert result.score >= 0.05


def test_score_ceiling_never_above_one():
    """Perfect inputs -> max 1.0."""
    criteria = [_make_criterion("c1", 1.0)]
    policy = _make_policy(criteria)
    evidence = [_make_evidence("c1", "MET", 1.0)]
    result = calculate_confidence(evidence, policy)
    assert result.score <= 1.0


def test_optional_not_met_reduces_score():
    """2 required MET + 2 optional NOT_MET should NOT produce 100%.

    Regression: low LLM confidence on NOT_MET criteria previously shrank
    the denominator, inflating the score to 1.0.
    """
    criteria = [
        _make_criterion("c1", 0.20, required=True),
        _make_criterion("c2", 0.35, required=True),
        _make_criterion("c3", 0.25, required=False),
        _make_criterion("c4", 0.20, required=False),
    ]
    policy = _make_policy(criteria)
    evidence = [
        _make_evidence("c1", "MET", 0.9),
        _make_evidence("c2", "MET", 0.9),
        _make_evidence("c3", "NOT_MET", 0.5),
        _make_evidence("c4", "NOT_MET", 0.5),
    ]
    result = calculate_confidence(evidence, policy)
    # With 45% of weight NOT_MET, score must be well below 1.0
    assert result.score < 0.80


def test_low_confidence_not_met_cannot_inflate_score():
    """NOT_MET with very low LLM confidence must not produce score > 0.80.

    Regression: when llm_conf approached 0 for NOT_MET criteria, the denominator
    collapsed and the score inflated to 1.0.
    """
    criteria = [
        _make_criterion("c1", 0.50, required=True),
        _make_criterion("c2", 0.50, required=False),
    ]
    policy = _make_policy(criteria)
    evidence = [
        _make_evidence("c1", "MET", 0.9),
        _make_evidence("c2", "NOT_MET", 0.1),  # very low confidence
    ]
    result = calculate_confidence(evidence, policy)
    assert result.score < 0.80
