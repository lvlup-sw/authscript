"""Tests for generic fallback policy builder."""
import pytest
from src.models.policy import PolicyDefinition
from src.policies.generic_policy import build_generic_policy


def test_build_generic_policy_returns_policy_definition():
    """Returns a PolicyDefinition instance."""
    result = build_generic_policy("99999")
    assert isinstance(result, PolicyDefinition)


def test_generic_policy_has_three_criteria():
    """Generic policy has 3 universal criteria."""
    result = build_generic_policy("99999")
    assert len(result.criteria) == 3
    ids = {c.id for c in result.criteria}
    assert ids == {"medical_necessity", "diagnosis_present", "conservative_therapy"}


def test_generic_policy_weights_sum_to_one():
    """Weights sum approximately to 1.0."""
    result = build_generic_policy("99999")
    total = sum(c.weight for c in result.criteria)
    assert total == pytest.approx(1.0, abs=0.01)


def test_generic_policy_no_lcd_reference():
    """Generic policy has no LCD reference."""
    result = build_generic_policy("99999")
    assert result.lcd_reference is None


def test_generic_policy_includes_procedure_code():
    """Passed CPT code appears in procedure_codes."""
    result = build_generic_policy("12345")
    assert "12345" in result.procedure_codes


def test_generic_policy_payer_is_general():
    """Payer field is set to a generic value."""
    result = build_generic_policy("99999")
    assert "general" in result.payer.lower() or "generic" in result.payer.lower()
