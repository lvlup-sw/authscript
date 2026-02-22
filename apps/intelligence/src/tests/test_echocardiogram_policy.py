"""Tests for echocardiogram seed policy."""
import pytest
from src.policies.registry import registry
from src.policies.seed.echocardiogram import POLICY as ECHOCARDIOGRAM


def test_echocardiogram_policy_registered():
    """93306 resolves to the echocardiogram seed policy, not generic."""
    resolved = registry.resolve("93306")
    assert resolved.policy_id == ECHOCARDIOGRAM.policy_id


def test_echocardiogram_policy_covers_all_tte_variants():
    """All transthoracic echo CPTs resolve to this policy."""
    for cpt in ["93303", "93304", "93306", "93307", "93308"]:
        resolved = registry.resolve(cpt)
        assert resolved.policy_id == ECHOCARDIOGRAM.policy_id, f"CPT {cpt} not resolved"


def test_echocardiogram_policy_criteria_count():
    """Echocardiogram policy has exactly 4 criteria."""
    assert len(ECHOCARDIOGRAM.criteria) == 4


def test_echocardiogram_policy_no_conservative_therapy():
    """No conservative_therapy criterion — echo is diagnostic, not therapeutic."""
    criterion_ids = [c.id for c in ECHOCARDIOGRAM.criteria]
    assert "conservative_therapy" not in criterion_ids
    assert "conservative_therapy_4wk" not in criterion_ids


def test_echocardiogram_policy_has_required_criteria():
    """diagnosis_present and clinical_indication are required."""
    required_ids = {c.id for c in ECHOCARDIOGRAM.criteria if c.required}
    assert required_ids == {"diagnosis_present", "clinical_indication"}


def test_echocardiogram_policy_weights_sum_to_one():
    """Weights sum to 1.0."""
    total = sum(c.weight for c in ECHOCARDIOGRAM.criteria)
    assert total == pytest.approx(1.0, abs=0.01)


def test_echocardiogram_policy_all_criteria_have_lcd_sections():
    """All criteria have non-null lcd_section."""
    for c in ECHOCARDIOGRAM.criteria:
        assert c.lcd_section is not None, f"{c.id} missing lcd_section"
