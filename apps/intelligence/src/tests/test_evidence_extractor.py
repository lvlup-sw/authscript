"""Tests for evidence extractor stub implementation."""

from datetime import date
from unittest.mock import AsyncMock, patch

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.reasoning.evidence_extractor import evaluate_criterion, extract_evidence
from src.models.pa_form import EvidenceItem


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


# --- A1: evaluate_criterion tests ---


@pytest.mark.asyncio
async def test_evaluate_criterion_returns_met_evidence_item():
    """Test that evaluate_criterion returns an EvidenceItem with MET status."""
    criterion = {"id": "crit-1", "description": "Patient has documented symptoms", "required": True}
    clinical_summary = "Patient presents with chronic lower back pain for 8 weeks."

    mock_llm = AsyncMock(return_value="Based on the clinical data, this criterion is MET. The patient has documented symptoms of chronic lower back pain.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, clinical_summary)

    assert isinstance(result, EvidenceItem)
    assert result.criterion_id == "crit-1"
    assert result.status == "MET"
    assert result.confidence == 0.8


@pytest.mark.asyncio
async def test_evaluate_criterion_parses_not_met():
    """Test that evaluate_criterion correctly parses NOT_MET response."""
    criterion = {"id": "crit-2", "description": "Conservative therapy completed", "required": True}
    clinical_summary = "Patient has not attempted physical therapy."

    mock_llm = AsyncMock(return_value="This criterion is NOT MET. No evidence of conservative therapy.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, clinical_summary)

    assert result.status == "NOT_MET"
    assert result.confidence == 0.8


@pytest.mark.asyncio
async def test_evaluate_criterion_handles_none_response():
    """Test that evaluate_criterion handles LLM returning None gracefully."""
    criterion = {"id": "crit-3", "description": "Valid diagnosis", "required": False}
    clinical_summary = "Patient data."

    mock_llm = AsyncMock(return_value=None)
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, clinical_summary)

    assert result.status == "UNCLEAR"
    assert result.confidence == 0.5


# --- A2: Parallel evidence extraction tests ---


@pytest.mark.asyncio
async def test_extract_evidence_calls_criteria_concurrently():
    """Test that evidence extraction runs criteria evaluation in parallel."""
    import asyncio
    import time

    call_times: list[float] = []

    async def slow_llm(*args, **kwargs):
        call_times.append(time.monotonic())
        await asyncio.sleep(0.1)  # Simulate LLM latency
        return "The criterion is MET based on clinical data."

    bundle = ClinicalBundle(
        patient_id="test-patient",
        patient=PatientInfo(name="Test Patient", birth_date=date(1990, 1, 1)),
        conditions=[Condition(code="M54.5", display="Low back pain")],
    )
    policy = {
        "name": "Test Policy",
        "criteria": [
            {"id": f"crit-{i}", "description": f"Criterion {i}", "required": True}
            for i in range(3)
        ],
        "procedure_codes": ["72148"],
    }

    mock_llm = AsyncMock(side_effect=slow_llm)
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        start = time.monotonic()
        results = await extract_evidence(bundle, policy)
        duration = time.monotonic() - start

    assert len(results) == 3
    # If parallel: ~0.1s. If sequential: ~0.3s. Allow margin.
    assert duration < 0.25, f"Expected parallel execution (<0.25s), got {duration:.2f}s"


@pytest.mark.asyncio
async def test_extract_evidence_respects_semaphore_limit():
    """Test that concurrent LLM calls are bounded by semaphore."""
    import asyncio

    max_concurrent = 0
    current_concurrent = 0
    lock = asyncio.Lock()

    async def counting_llm(*args, **kwargs):
        nonlocal max_concurrent, current_concurrent
        async with lock:
            current_concurrent += 1
            max_concurrent = max(max_concurrent, current_concurrent)
        await asyncio.sleep(0.05)
        async with lock:
            current_concurrent -= 1
        return "The criterion is MET."

    bundle = ClinicalBundle(
        patient_id="test-patient",
        patient=PatientInfo(name="Test", birth_date=date(1990, 1, 1)),
        conditions=[Condition(code="M54.5", display="Low back pain")],
    )
    policy = {
        "name": "Test",
        "criteria": [
            {"id": f"crit-{i}", "description": f"Criterion {i}", "required": True}
            for i in range(6)
        ],
        "procedure_codes": ["72148"],
    }

    mock_llm = AsyncMock(side_effect=counting_llm)
    with (
        patch("src.reasoning.evidence_extractor.chat_completion", mock_llm),
        patch("src.reasoning.evidence_extractor._get_llm_semaphore", return_value=asyncio.Semaphore(2)),
    ):
        results = await extract_evidence(bundle, policy)

    assert len(results) == 6
    assert max_concurrent <= 2, f"Expected max 2 concurrent, got {max_concurrent}"


# --- M1: Semaphore singleton tests ---


def test_get_llm_semaphore_returns_singleton():
    """Semaphore should be the same instance across calls."""
    import src.reasoning.evidence_extractor as mod
    from src.reasoning.evidence_extractor import _get_llm_semaphore

    # Reset the cached semaphore
    mod._llm_semaphore = None

    sem1 = _get_llm_semaphore()
    sem2 = _get_llm_semaphore()
    assert sem1 is sem2, "Semaphore should be a singleton"

    # Cleanup
    mod._llm_semaphore = None
