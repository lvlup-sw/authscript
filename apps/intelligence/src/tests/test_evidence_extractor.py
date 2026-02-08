"""Tests for evidence extractor stub implementation."""

from unittest.mock import AsyncMock, patch

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.reasoning.evidence_extractor import extract_evidence


@pytest.fixture
def sample_bundle() -> ClinicalBundle:
    """Create a sample clinical bundle for testing."""
    return ClinicalBundle(
        patient_id="test-123",
        patient=PatientInfo(name="Test Patient"),
        conditions=[Condition(code="M54.5", display="Low back pain")],
    )


@pytest.fixture
def sample_policy() -> dict:
    """Create a sample policy with criteria."""
    return {
        "id": "test-policy",
        "criteria": [
            {"id": "crit-1", "description": "Test criterion 1"},
            {"id": "crit-2", "description": "Test criterion 2"},
        ],
    }


@pytest.mark.asyncio
async def test_extract_evidence_returns_met_for_all_criteria(
    sample_bundle: ClinicalBundle,
    sample_policy: dict,
) -> None:
    """Stub should return MET status for all policy criteria."""
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        evidence = await extract_evidence(sample_bundle, sample_policy)

    assert len(evidence) == 2
    assert all(e.status == "MET" for e in evidence)
    assert evidence[0].criterion_id == "crit-1"
    assert evidence[1].criterion_id == "crit-2"


@pytest.mark.asyncio
async def test_extract_evidence_empty_criteria() -> None:
    """Stub should return empty list when no criteria defined."""
    bundle = ClinicalBundle(patient_id="test")
    policy: dict = {"id": "empty", "criteria": []}

    evidence = await extract_evidence(bundle, policy)

    assert evidence == []


@pytest.mark.asyncio
async def test_extract_evidence_confidence_score(
    sample_bundle: ClinicalBundle,
    sample_policy: dict,
) -> None:
    """Stub should return 0.80 confidence for all items."""
    mock_llm = AsyncMock(return_value="The criterion is MET based on the evidence.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        evidence = await extract_evidence(sample_bundle, sample_policy)

    assert all(e.confidence == 0.80 for e in evidence)
