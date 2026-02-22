"""Tests for form generator implementation."""

from datetime import date
from unittest.mock import AsyncMock, patch

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.models.pa_form import EvidenceItem
from src.models.policy import PolicyCriterion, PolicyDefinition
from src.reasoning.confidence_scorer import ScoreResult
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
def sample_policy() -> PolicyDefinition:
    """Create a sample policy."""
    return PolicyDefinition(
        policy_id="test-policy",
        policy_name="Test Policy",
        payer="Test Payer",
        procedure_codes=["72148"],
        criteria=[
            PolicyCriterion(id="crit-1", description="Test criterion", weight=1.0),
        ],
    )


@pytest.mark.asyncio
async def test_generate_form_data_returns_approve(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Should return APPROVE recommendation via scorer."""
    mock_scorer = ScoreResult(score=0.9, recommendation="APPROVE")
    mock_llm = AsyncMock(return_value="Patient requires this procedure.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.recommendation == "APPROVE"
    assert result.confidence_score == 0.9


@pytest.mark.asyncio
async def test_generate_form_data_extracts_patient_info(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Should extract patient information from bundle."""
    mock_scorer = ScoreResult(score=0.85, recommendation="APPROVE")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.patient_name == "John Doe"
    assert result.patient_dob == "1980-05-15"
    assert result.member_id == "MEM-001"


@pytest.mark.asyncio
async def test_generate_form_data_extracts_diagnosis(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Should extract diagnosis codes from bundle."""
    mock_scorer = ScoreResult(score=0.85, recommendation="APPROVE")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.diagnosis_codes == ["M54.5"]


@pytest.mark.asyncio
async def test_generate_form_data_uses_policy_procedure_code(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Should use procedure code from policy."""
    mock_scorer = ScoreResult(score=0.85, recommendation="APPROVE")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert result.procedure_code == "72148"


@pytest.mark.asyncio
async def test_generate_form_data_handles_missing_patient() -> None:
    """Should handle missing patient data gracefully."""
    bundle = ClinicalBundle(patient_id="test")
    evidence: list[EvidenceItem] = []
    policy = PolicyDefinition(
        policy_id="test-empty",
        policy_name="Test Empty",
        payer="Test",
        procedure_codes=["72148"],
        criteria=[],
    )

    mock_scorer = ScoreResult(score=0.5, recommendation="MANUAL_REVIEW")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(bundle, evidence, policy)

    assert result.patient_name == "Unknown"
    assert result.patient_dob == "Unknown"
    assert result.member_id == "Unknown"


@pytest.mark.asyncio
async def test_generate_form_data_handles_empty_procedure_codes() -> None:
    """Should use default procedure code when list is empty."""
    bundle = ClinicalBundle(patient_id="test")
    evidence: list[EvidenceItem] = []
    policy = PolicyDefinition(
        policy_id="test-no-codes",
        policy_name="Test No Codes",
        payer="Test",
        procedure_codes=[],
        criteria=[],
    )

    mock_scorer = ScoreResult(score=0.5, recommendation="MANUAL_REVIEW")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(bundle, evidence, policy)

    assert result.procedure_code == "72148"


@pytest.mark.asyncio
async def test_generate_form_data_delegates_to_scorer(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Mock confidence_scorer, verify it's called."""
    mock_scorer = ScoreResult(score=0.72, recommendation="MANUAL_REVIEW")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer) as mock_calc,
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)
    mock_calc.assert_called_once()


@pytest.mark.asyncio
async def test_generate_form_data_uses_scorer_recommendation(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Scorer returns MANUAL_REVIEW -> response has MANUAL_REVIEW."""
    mock_scorer = ScoreResult(score=0.72, recommendation="MANUAL_REVIEW")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)
    assert result.recommendation == "MANUAL_REVIEW"


@pytest.mark.asyncio
async def test_generate_form_data_uses_scorer_confidence(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """Scorer returns 0.72 -> response.confidence_score == 0.72."""
    mock_scorer = ScoreResult(score=0.72, recommendation="MANUAL_REVIEW")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, sample_policy)
    assert result.confidence_score == 0.72


@pytest.mark.asyncio
async def test_generate_form_data_includes_policy_metadata(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
) -> None:
    """Response has policy_id + lcd_reference from policy."""
    policy = PolicyDefinition(
        policy_id="lcd-test-L12345",
        policy_name="Test LCD",
        lcd_reference="L12345",
        payer="CMS",
        procedure_codes=["72148"],
        criteria=[PolicyCriterion(id="c1", description="Test", weight=1.0)],
    )
    mock_scorer = ScoreResult(score=0.85, recommendation="APPROVE")
    mock_llm = AsyncMock(return_value="Summary.")
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        result = await generate_form_data(sample_bundle, sample_evidence, policy)
    assert result.policy_id == "lcd-test-L12345"
    assert result.lcd_reference == "L12345"


@pytest.mark.asyncio
async def test_generate_form_data_no_redacted_in_prompt(
    sample_bundle: ClinicalBundle,
    sample_evidence: list[EvidenceItem],
    sample_policy: PolicyDefinition,
) -> None:
    """LLM prompt must not contain '[REDACTED]' text.

    Regression: 'Patient: [REDACTED]' in the prompt caused the LLM to
    echo it back into the user-facing clinical summary.
    """
    captured_prompts: list[str] = []

    async def capture_llm(*args, **kwargs):
        captured_prompts.append(kwargs.get("user_prompt", ""))
        return "Clinical summary here."

    mock_scorer = ScoreResult(score=0.85, recommendation="APPROVE")
    mock_llm = AsyncMock(side_effect=capture_llm)
    with (
        patch("src.reasoning.form_generator.calculate_confidence", return_value=mock_scorer),
        patch("src.reasoning.form_generator.chat_completion", mock_llm),
    ):
        await generate_form_data(sample_bundle, sample_evidence, sample_policy)

    assert captured_prompts, "LLM should have been called"
    for prompt in captured_prompts:
        assert "[REDACTED]" not in prompt
