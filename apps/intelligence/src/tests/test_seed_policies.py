"""Tests for LCD-backed seed policies."""
import pytest
from src.policies.seed.mri_lumbar import POLICY as MRI_LUMBAR
from src.policies.seed.mri_brain import POLICY as MRI_BRAIN
from src.policies.seed.tka import POLICY as TKA
from src.policies.seed.physical_therapy import POLICY as PHYSICAL_THERAPY
from src.policies.seed.epidural_steroid import POLICY as EPIDURAL_STEROID
from src.policies.seed.echocardiogram import POLICY as ECHOCARDIOGRAM


ALL_POLICIES = [MRI_LUMBAR, MRI_BRAIN, TKA, PHYSICAL_THERAPY, EPIDURAL_STEROID, ECHOCARDIOGRAM]


def test_mri_lumbar_lcd_reference():
    assert MRI_LUMBAR.lcd_reference == "L34220"

def test_mri_lumbar_has_five_criteria():
    assert len(MRI_LUMBAR.criteria) == 5

def test_mri_lumbar_conservative_therapy_bypass():
    """red_flag_screening bypasses conservative_therapy_4wk."""
    red_flag = next(c for c in MRI_LUMBAR.criteria if c.id == "red_flag_screening")
    assert "conservative_therapy_4wk" in red_flag.bypasses

def test_mri_brain_lcd_reference():
    assert MRI_BRAIN.lcd_reference == "L37373"

def test_mri_brain_has_four_criteria():
    assert len(MRI_BRAIN.criteria) == 4

def test_tka_lcd_reference():
    assert TKA.lcd_reference == "L36575"

def test_tka_has_five_criteria():
    assert len(TKA.criteria) == 5

def test_physical_therapy_lcd_reference():
    assert PHYSICAL_THERAPY.lcd_reference == "L34049"

def test_epidural_lcd_reference():
    assert EPIDURAL_STEROID.lcd_reference == "L39240"

@pytest.mark.parametrize("policy", ALL_POLICIES, ids=lambda p: p.policy_id)
def test_all_seed_weights_valid(policy):
    """All weights in [0,1] and sum to ~1.0."""
    total = sum(c.weight for c in policy.criteria)
    assert total == pytest.approx(1.0, abs=0.01)
    for c in policy.criteria:
        assert 0.0 <= c.weight <= 1.0

@pytest.mark.parametrize("policy", ALL_POLICIES, ids=lambda p: p.policy_id)
def test_all_seed_criteria_have_lcd_sections(policy):
    """All criteria have non-null lcd_section."""
    for c in policy.criteria:
        assert c.lcd_section is not None, f"{policy.policy_id}.{c.id} missing lcd_section"
