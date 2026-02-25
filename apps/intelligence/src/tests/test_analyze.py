"""Tests for analyze API endpoint implementation."""

from unittest.mock import AsyncMock, patch

import pytest
from fastapi import HTTPException

from src.api.analyze import AnalyzeRequest, analyze


@pytest.fixture
def valid_request() -> AnalyzeRequest:
    """Create a valid analyze request."""
    return AnalyzeRequest(
        patient_id="test-123",
        procedure_code="72148",
        clinical_data={
            "patient": {
                "name": "John Doe",
                "birth_date": "1980-05-15",
                "member_id": "MEM-001",
            },
            "conditions": [
                {"code": "M54.5", "display": "Low back pain"},
            ],
        },
    )


@pytest.mark.asyncio
async def test_analyze_returns_approve(valid_request: AnalyzeRequest) -> None:
    """Should return APPROVE recommendation with high confidence."""
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence. HIGH CONFIDENCE.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(valid_request)

    assert result.recommendation == "APPROVE"
    assert result.confidence_score >= 0.80  # Weighted score, not fixed 0.9


@pytest.mark.asyncio
async def test_analyze_extracts_patient_info(valid_request: AnalyzeRequest) -> None:
    """Should extract patient information."""
    mock_llm = AsyncMock(return_value="MET. Evidence found.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(valid_request)

    assert result.patient_name == "John Doe"
    assert result.patient_dob == "1980-05-15"
    assert result.member_id == "MEM-001"


@pytest.mark.asyncio
async def test_analyze_requires_patient_dob() -> None:
    """Should require patient birth_date."""
    request = AnalyzeRequest(
        patient_id="test",
        procedure_code="72148",
        clinical_data={"patient": {"name": "Test"}},
    )

    with pytest.raises(HTTPException) as exc_info:
        await analyze(request)

    assert exc_info.value.status_code == 400
    assert "birth_date" in exc_info.value.detail


@pytest.mark.asyncio
async def test_analyze_builds_field_mappings(valid_request: AnalyzeRequest) -> None:
    """Should include PDF field mappings."""
    mock_llm = AsyncMock(return_value="MET. Evidence found.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(valid_request)

    assert "PatientName" in result.field_mappings
    assert "PatientDOB" in result.field_mappings
    assert "ProcedureCode" in result.field_mappings
    assert result.field_mappings["PatientName"] == "John Doe"


@pytest.mark.asyncio
async def test_analyze_unknown_cpt_returns_200_with_generic() -> None:
    """CPT 99999 -> 200 OK with generic policy (no lcd_reference)."""
    request = AnalyzeRequest(
        patient_id="test",
        procedure_code="99999",
        clinical_data={"patient": {"name": "Test", "birth_date": "1980-01-01", "member_id": "M001"}},
    )
    mock_llm = AsyncMock(return_value="MET. Evidence found.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request)
    assert result.lcd_reference is None  # Generic fallback
    assert result.recommendation in ("APPROVE", "MANUAL_REVIEW", "NEED_INFO")


@pytest.mark.asyncio
async def test_analyze_mri_lumbar_uses_lcd_policy() -> None:
    """CPT 72148 -> response includes lcd_reference='L34220'."""
    request = AnalyzeRequest(
        patient_id="test",
        procedure_code="72148",
        clinical_data={
            "patient": {"name": "Test", "birth_date": "1980-01-01", "member_id": "M001"},
            "conditions": [{"code": "M54.5", "display": "Low back pain"}],
        },
    )
    mock_llm = AsyncMock(return_value="MET. HIGH CONFIDENCE. Evidence found.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request)
    assert result.lcd_reference == "L34220"
    assert result.policy_id == "lcd-mri-lumbar-L34220"


@pytest.mark.asyncio
async def test_analyze_demo_flag_returns_canned_response() -> None:
    """demo=True with CPT 72148 returns canned high-confidence response."""
    request = AnalyzeRequest(
        patient_id="test-demo",
        procedure_code="72148",
        clinical_data={
            "patient": {"name": "Demo Patient", "birth_date": "1975-03-20"},
        },
    )
    result = await analyze(request, demo=True)

    assert result.recommendation == "APPROVE"
    assert result.confidence_score >= 0.85
    assert len(result.supporting_evidence) == 5
    assert all(item.status == "MET" for item in result.supporting_evidence)
    assert len(result.clinical_summary) > 0
    # Verify criterion IDs match real LCD L34220 policy
    criterion_ids = {item.criterion_id for item in result.supporting_evidence}
    assert criterion_ids == {
        "diagnosis_present",
        "red_flag_screening",
        "conservative_therapy_4wk",
        "clinical_rationale",
        "no_duplicate_imaging",
    }
    assert result.policy_id == "lcd-mri-lumbar-L34220"
    assert result.lcd_reference == "L34220"


@pytest.mark.asyncio
async def test_analyze_demo_flag_ignored_for_non_demo_procedure() -> None:
    """demo=True with non-72148 CPT should NOT use canned demo response."""
    request = AnalyzeRequest(
        patient_id="test-demo",
        procedure_code="99213",
        clinical_data={
            "patient": {"name": "Demo Patient", "birth_date": "1975-03-20", "member_id": "M999"},
        },
    )
    mock_llm = AsyncMock(return_value="MET. Evidence found.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request, demo=True)

    # Should have gone through normal pipeline — verify LLM was called
    mock_llm.assert_called()
    # Demo fixture uses LCD L34220 criteria; normal pipeline should not
    criterion_ids = {item.criterion_id for item in result.supporting_evidence}
    assert "diagnosis_present" not in criterion_ids or result.policy_id != "lcd-mri-lumbar-L34220"
