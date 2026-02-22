"""Tests for evidence extractor stub implementation."""

from datetime import date
from unittest.mock import AsyncMock, patch

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.models.pa_form import EvidenceItem
from src.reasoning.evidence_extractor import evaluate_criterion, extract_evidence


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

    assert all(e.confidence == 0.7 for e in evidence)


# --- A1: evaluate_criterion tests ---


@pytest.mark.asyncio
async def test_evaluate_criterion_returns_met_evidence_item():
    """Test that evaluate_criterion returns an EvidenceItem with MET status."""
    criterion = {"id": "crit-1", "description": "Patient has documented symptoms", "required": True}
    clinical_summary = "Patient presents with chronic lower back pain for 8 weeks."

    mock_llm = AsyncMock(
        return_value="Based on the clinical data, this criterion is MET."
        " The patient has documented symptoms of chronic lower back pain."
    )
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, clinical_summary)

    assert isinstance(result, EvidenceItem)
    assert result.criterion_id == "crit-1"
    assert result.status == "MET"
    assert result.confidence == 0.7


@pytest.mark.asyncio
async def test_evaluate_criterion_parses_not_met():
    """Test that evaluate_criterion correctly parses NOT_MET response."""
    criterion = {"id": "crit-2", "description": "Conservative therapy completed", "required": True}
    clinical_summary = "Patient has not attempted physical therapy."

    mock_llm = AsyncMock(
        return_value="This criterion is NOT MET."
        " No evidence of conservative therapy."
    )
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, clinical_summary)

    assert result.status == "NOT_MET"
    assert result.confidence == 0.7


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
        patch(
            "src.reasoning.evidence_extractor._get_llm_semaphore",
            return_value=asyncio.Semaphore(2),
        ),
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


# --- T006: Evidence extractor enhancement tests ---

from src.models.policy import PolicyCriterion, PolicyDefinition


def _make_policy_def() -> PolicyDefinition:
    return PolicyDefinition(
        policy_id="test-lcd",
        policy_name="Test LCD Policy",
        payer="CMS Medicare",
        procedure_codes=["72148"],
        criteria=[
            PolicyCriterion(
                id="crit-1", description="Test criterion 1", weight=0.5,
                lcd_section="L34220 — Test Section",
            ),
            PolicyCriterion(
                id="crit-2", description="Test criterion 2", weight=0.5,
                lcd_section="L34220 — Another Section",
            ),
        ],
    )


@pytest.mark.asyncio
async def test_extract_evidence_accepts_policy_definition():
    """Pass PolicyDefinition instead of dict -> works."""
    policy = _make_policy_def()
    bundle = ClinicalBundle(
        patient_id="test",
        patient=PatientInfo(name="Test"),
        conditions=[Condition(code="M54.5", display="Low back pain")],
    )
    mock_llm = AsyncMock(return_value="The criterion is MET based on clinical data.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        evidence = await extract_evidence(bundle, policy)
    assert len(evidence) == 2
    assert evidence[0].criterion_id == "crit-1"


@pytest.mark.asyncio
async def test_evaluate_criterion_includes_lcd_section_in_prompt():
    """Mock LLM captures prompt, verify LCD section text present."""
    criterion = PolicyCriterion(
        id="test", description="Test criterion", weight=0.5,
        lcd_section="L34220 — Coverage Principle",
    )
    captured_prompts = []

    async def capture_llm(*args, **kwargs):
        captured_prompts.append(kwargs.get("user_prompt", args[1] if len(args) > 1 else ""))
        return "MET. HIGH CONFIDENCE. Evidence found."

    mock_llm = AsyncMock(side_effect=capture_llm)
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        await evaluate_criterion(criterion, "Clinical data here")
    assert any("L34220" in p for p in captured_prompts)


@pytest.mark.asyncio
async def test_evaluate_criterion_confidence_parsing_high():
    """LLM response with 'HIGH CONFIDENCE' -> conf=0.9."""
    criterion = {"id": "test", "description": "Test"}
    mock_llm = AsyncMock(return_value="MET. HIGH CONFIDENCE. Strong evidence.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, "data")
    assert result.confidence == 0.9


@pytest.mark.asyncio
async def test_evaluate_criterion_confidence_parsing_low():
    """LLM response with 'LOW CONFIDENCE' -> conf=0.5."""
    criterion = {"id": "test", "description": "Test"}
    mock_llm = AsyncMock(return_value="UNCLEAR. LOW CONFIDENCE. Limited data.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, "data")
    assert result.confidence == 0.5


@pytest.mark.asyncio
async def test_evaluate_criterion_confidence_parsing_default():
    """No confidence signal -> conf=0.7."""
    criterion = {"id": "test", "description": "Test"}
    mock_llm = AsyncMock(return_value="MET. Evidence found in records.")
    with patch("src.reasoning.evidence_extractor.chat_completion", mock_llm):
        result = await evaluate_criterion(criterion, "data")
    assert result.confidence == 0.7


def test_clinical_summary_no_redacted_text():
    """Clinical summary must not contain '[REDACTED]' text.

    Regression: 'Patient: [REDACTED]' leaked into the LLM prompt and was
    echoed back into the user-facing clinical summary.
    """
    from src.reasoning.evidence_extractor import _build_clinical_summary

    bundle = ClinicalBundle(
        patient_id="test",
        patient=PatientInfo(name="Jane Doe", birth_date=date(1960, 3, 15)),
        conditions=[Condition(code="I50.32", display="Heart failure")],
    )
    summary = _build_clinical_summary(bundle)
    assert "[REDACTED]" not in summary
