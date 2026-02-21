"""Tests for analyze API endpoint stub implementation."""

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
    """Stub should return APPROVE recommendation."""
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(valid_request)

    assert result.recommendation == "APPROVE"
    assert result.confidence_score == 0.9


@pytest.mark.asyncio
async def test_analyze_extracts_patient_info(valid_request: AnalyzeRequest) -> None:
    """Stub should extract patient information."""
    result = await analyze(valid_request)

    assert result.patient_name == "John Doe"
    assert result.patient_dob == "1980-05-15"
    assert result.member_id == "MEM-001"


@pytest.mark.asyncio
async def test_analyze_unsupported_procedure_uses_generic_fallback() -> None:
    """Unsupported procedure codes now fall back to generic policy instead of 400."""
    request = AnalyzeRequest(
        patient_id="test",
        procedure_code="99999",
        clinical_data={"patient": {"name": "Test", "birth_date": "1980-01-01"}},
    )
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request)
    assert result.procedure_code == "99999"


@pytest.mark.asyncio
async def test_analyze_requires_patient_dob() -> None:
    """Stub should require patient birth_date."""
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
    """Stub should include PDF field mappings."""
    result = await analyze(valid_request)

    assert "PatientName" in result.field_mappings
    assert "PatientDOB" in result.field_mappings
    assert "ProcedureCode" in result.field_mappings
    assert result.field_mappings["PatientName"] == "John Doe"


@pytest.mark.asyncio
async def test_analyze_unsupported_procedure_uses_generic_policy():
    """POST /analyze with unsupported procedure returns 200, not 400."""
    request = AnalyzeRequest(
        patient_id="test-123",
        procedure_code="27447",  # Total Knee Replacement - not in SUPPORTED_PROCEDURE_CODES
        clinical_data={
            "patient": {"name": "Test Patient", "birth_date": "1968-03-15", "member_id": "MEM001"},
            "conditions": [{"code": "M17.11", "display": "Primary OA Right Knee"}],
            "observations": [],
            "procedures": [],
        },
    )
    mock_llm = AsyncMock(return_value="The criterion is MET based on the clinical evidence provided.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request)
    assert result.procedure_code == "27447"


@pytest.mark.asyncio
async def test_analyze_generic_policy_returns_valid_response():
    """Generic policy response has all required PAFormResponse fields."""
    request = AnalyzeRequest(
        patient_id="test-456",
        procedure_code="43239",  # Upper GI Endoscopy
        clinical_data={
            "patient": {"name": "Jane Doe", "birth_date": "1975-07-22", "member_id": "MEM002"},
            "conditions": [{"code": "K21.0", "display": "GERD with Esophagitis"}],
            "observations": [],
            "procedures": [],
        },
    )
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request)
    assert result.patient_name is not None
    assert result.confidence_score >= 0
    assert len(result.supporting_evidence) > 0


@pytest.mark.asyncio
async def test_analyze_known_procedure_still_uses_specific_policy():
    """72148 (MRI Lumbar) still uses the specific MRI policy, not generic."""
    request = AnalyzeRequest(
        patient_id="test-789",
        procedure_code="72148",
        clinical_data={
            "patient": {"name": "Bob Smith", "birth_date": "1982-11-08", "member_id": "MEM003"},
            "conditions": [{"code": "M54.5", "display": "Low Back Pain"}],
            "observations": [],
            "procedures": [],
        },
    )
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence.")
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await analyze(request)
    # MRI policy has conservative_therapy criterion; generic doesn't
    evidence_ids = [e.criterion_id for e in result.supporting_evidence]
    assert "conservative_therapy" in evidence_ids
