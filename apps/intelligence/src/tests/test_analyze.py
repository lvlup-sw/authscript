"""Tests for analyze API endpoint stub implementation."""

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
    result = await analyze(valid_request)

    assert result.recommendation == "APPROVE"
    assert result.confidence_score == 1.0


@pytest.mark.asyncio
async def test_analyze_extracts_patient_info(valid_request: AnalyzeRequest) -> None:
    """Stub should extract patient information."""
    result = await analyze(valid_request)

    assert result.patient_name == "John Doe"
    assert result.patient_dob == "1980-05-15"
    assert result.member_id == "MEM-001"


@pytest.mark.asyncio
async def test_analyze_rejects_unsupported_procedure() -> None:
    """Stub should reject unsupported procedure codes."""
    request = AnalyzeRequest(
        patient_id="test",
        procedure_code="99999",
        clinical_data={"patient": {"name": "Test", "birth_date": "1980-01-01"}},
    )

    with pytest.raises(HTTPException) as exc_info:
        await analyze(request)

    assert exc_info.value.status_code == 400
    assert "not supported" in exc_info.value.detail


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
