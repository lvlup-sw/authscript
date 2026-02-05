"""Tests for form generator stub implementation."""

from datetime import date
from unittest.mock import AsyncMock, patch

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.models.pa_form import EvidenceItem
from src.reasoning.form_generator import generate_form_data


@pytest.fixture
def sample_bundle() -> ClinicalBundle:
    """Create a sample clinical bundle for testing."""
    return ClinicalBundle(
        patient_id="test-123",
        patient=PatientInfo(
            name="John Doe",
            birth_date=date(1980, 5, 15),
            member_id="MEM-001",
        ),
        conditions=[Condition(code="M54.5", display="Low back pain")],
    )


@pytest.fixture
def sample_evidence() -> list[EvidenceItem]:
    """Create sample evidence items."""
    return [
        EvidenceItem(
            criterion_id="crit-1",
            status="MET",
            evidence="Test evidence",
            source="Test",
            confidence=0.90,
        )
    ]


@pytest.fixture
def sample_policy() -> dict:
    """Create a sample policy."""
    return {
        "id": "test-policy",
        "procedure_codes": ["72148"],
    }


@pytest.mark.asyncio
async def test_generate_form_data_returns_approve(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: dict,
) -> None:
    """Stub should return APPROVE recommendation."""
    mock_llm = AsyncMock(return_value="Patient requires this procedure.")
    with patch("src.reasoning.form_generator.chat_completion", mock_llm):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.recommendation == "APPROVE"
    assert result.confidence_score == 0.9


@pytest.mark.asyncio
async def test_generate_form_data_extracts_patient_info(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: dict,
) -> None:
    """Stub should extract patient information from bundle."""
    result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.patient_name == "John Doe"
    assert result.patient_dob == "1980-05-15"
    assert result.member_id == "MEM-001"


@pytest.mark.asyncio
async def test_generate_form_data_extracts_diagnosis(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: dict,
) -> None:
    """Stub should extract diagnosis codes from bundle."""
    result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.diagnosis_codes == ["M54.5"]


@pytest.mark.asyncio
async def test_generate_form_data_uses_policy_procedure_code(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: dict,
) -> None:
    """Stub should use procedure code from policy."""
    result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.procedure_code == "72148"


@pytest.mark.asyncio
async def test_generate_form_data_handles_missing_patient() -> None:
    """Stub should handle missing patient data gracefully."""
    bundle = ClinicalBundle(patient_id="test")
    evidence: list[EvidenceItem] = []
    policy: dict = {"procedure_codes": ["72148"]}

    result = await generate_form_data(bundle, evidence, policy)

    assert result.patient_name == "Unknown"
    assert result.patient_dob == "Unknown"
    assert result.member_id == "Unknown"


@pytest.mark.asyncio
async def test_generate_form_data_handles_empty_procedure_codes() -> None:
    """Stub should use default procedure code when list is empty."""
    bundle = ClinicalBundle(patient_id="test")
    evidence: list[EvidenceItem] = []
    policy: dict = {"procedure_codes": []}

    result = await generate_form_data(bundle, evidence, policy)

    assert result.procedure_code == "72148"
