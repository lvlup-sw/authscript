"""Tests for generic policy builder."""
import pytest
from src.policies.generic_policy import build_generic_policy


def test_build_generic_policy_returns_valid_structure():
    """Policy has all required keys matching EXAMPLE_POLICY structure."""
    policy = build_generic_policy("27447")
    assert "policy_id" in policy
    assert "criteria" in policy
    assert "form_field_mappings" in policy
    assert "procedure_codes" in policy
    assert "diagnosis_codes" in policy


def test_build_generic_policy_includes_medical_necessity_criterion():
    """Generic policy always includes medical necessity criterion."""
    policy = build_generic_policy("27447")
    criterion_ids = [c["id"] for c in policy["criteria"]]
    assert "medical_necessity" in criterion_ids


def test_build_generic_policy_includes_diagnosis_criterion():
    """Generic policy always includes diagnosis validation criterion."""
    policy = build_generic_policy("27447")
    criterion_ids = [c["id"] for c in policy["criteria"]]
    assert "diagnosis_present" in criterion_ids


def test_build_generic_policy_uses_procedure_code():
    """Procedure code is set in the policy."""
    policy = build_generic_policy("27447")
    assert "27447" in policy["procedure_codes"]


def test_build_generic_policy_criteria_have_required_fields():
    """Each criterion has id, description, and required fields."""
    policy = build_generic_policy("99999")
    for criterion in policy["criteria"]:
        assert "id" in criterion
        assert "description" in criterion
        assert "required" in criterion
