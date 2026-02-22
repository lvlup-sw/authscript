"""Tests for PAFormResponse model update."""
import pytest
from src.models.pa_form import PAFormResponse


def test_pa_form_response_backward_compat():
    """Construct without new fields -> defaults to None."""
    resp = PAFormResponse(
        patient_name="Test", patient_dob="2000-01-01", member_id="M001",
        diagnosis_codes=["M54.5"], procedure_code="72148",
        clinical_summary="Summary", supporting_evidence=[],
        recommendation="APPROVE", confidence_score=0.9,
        field_mappings={"PatientName": "Test"},
    )
    assert resp.policy_id is None
    assert resp.lcd_reference is None


def test_pa_form_response_with_policy_metadata():
    """Construct with policy_id + lcd_reference -> present."""
    resp = PAFormResponse(
        patient_name="Test", patient_dob="2000-01-01", member_id="M001",
        diagnosis_codes=["M54.5"], procedure_code="72148",
        clinical_summary="Summary", supporting_evidence=[],
        recommendation="APPROVE", confidence_score=0.9,
        field_mappings={"PatientName": "Test"},
        policy_id="lcd-mri-lumbar-L34220",
        lcd_reference="L34220",
    )
    assert resp.policy_id == "lcd-mri-lumbar-L34220"
    assert resp.lcd_reference == "L34220"


def test_pa_form_response_serialization_includes_new_fields():
    """model_dump() includes policy_id and lcd_reference."""
    resp = PAFormResponse(
        patient_name="Test", patient_dob="2000-01-01", member_id="M001",
        diagnosis_codes=["M54.5"], procedure_code="72148",
        clinical_summary="Summary", supporting_evidence=[],
        recommendation="APPROVE", confidence_score=0.9,
        field_mappings={},
        policy_id="test", lcd_reference="L12345",
    )
    data = resp.model_dump()
    assert data["policy_id"] == "test"
    assert data["lcd_reference"] == "L12345"


def test_pa_form_response_serialization_omits_none():
    """With exclude_none, absent fields omitted."""
    resp = PAFormResponse(
        patient_name="Test", patient_dob="2000-01-01", member_id="M001",
        diagnosis_codes=["M54.5"], procedure_code="72148",
        clinical_summary="Summary", supporting_evidence=[],
        recommendation="APPROVE", confidence_score=0.9,
        field_mappings={},
    )
    data = resp.model_dump(exclude_none=True)
    assert "policy_id" not in data
    assert "lcd_reference" not in data
