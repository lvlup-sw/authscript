"""Tests for evidence extraction."""

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.policies.mri_lumbar import MRI_LUMBAR_POLICY
from src.reasoning.evidence_extractor import extract_evidence


@pytest.fixture
def sample_bundle() -> ClinicalBundle:
    """Create a sample clinical bundle for testing."""
    return ClinicalBundle(
        patient_id="test-001",
        patient=PatientInfo(
            name="John Doe",
            member_id="MEM123456",
        ),
        conditions=[
            Condition(
                code="M54.5",
                display="Low back pain",
                clinical_status="active",
            ),
        ],
    )


@pytest.fixture
def bundle_with_radiculopathy() -> ClinicalBundle:
    """Bundle with neurological symptoms."""
    return ClinicalBundle(
        patient_id="test-002",
        patient=PatientInfo(name="Jane Smith"),
        conditions=[
            Condition(
                code="M51.16",
                display="Intervertebral disc disorder with radiculopathy, lumbar region",
                clinical_status="active",
            ),
        ],
    )


@pytest.mark.asyncio
async def test_diagnosis_check_with_primary_code(sample_bundle: ClinicalBundle) -> None:
    """Test that primary diagnosis codes are detected."""
    evidence = await extract_evidence(sample_bundle, MRI_LUMBAR_POLICY)

    diagnosis_evidence = next(
        (e for e in evidence if e.criterion_id == "diagnosis_present"), None
    )

    assert diagnosis_evidence is not None
    assert diagnosis_evidence.status == "MET"
    assert "M54.5" in diagnosis_evidence.evidence


@pytest.mark.asyncio
async def test_diagnosis_check_with_supporting_code(
    bundle_with_radiculopathy: ClinicalBundle,
) -> None:
    """Test that supporting diagnosis codes are detected."""
    evidence = await extract_evidence(bundle_with_radiculopathy, MRI_LUMBAR_POLICY)

    diagnosis_evidence = next(
        (e for e in evidence if e.criterion_id == "diagnosis_present"), None
    )

    assert diagnosis_evidence is not None
    assert diagnosis_evidence.status == "MET"


@pytest.mark.asyncio
async def test_missing_diagnosis() -> None:
    """Test behavior when no qualifying diagnosis is present."""
    bundle = ClinicalBundle(
        patient_id="test-003",
        conditions=[
            Condition(code="Z00.00", display="General health exam"),
        ],
    )

    evidence = await extract_evidence(bundle, MRI_LUMBAR_POLICY)

    diagnosis_evidence = next(
        (e for e in evidence if e.criterion_id == "diagnosis_present"), None
    )

    assert diagnosis_evidence is not None
    assert diagnosis_evidence.status == "NOT_MET"
