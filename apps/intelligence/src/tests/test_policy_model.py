"""Tests for policy data models."""
import pytest
from src.models.policy import PolicyCriterion, PolicyDefinition


def test_policy_criterion_valid():
    """Construct PolicyCriterion with all fields."""
    c = PolicyCriterion(
        id="conservative_therapy",
        description="6+ weeks conservative therapy",
        weight=0.30,
        required=True,
        lcd_section="L34220 §4.2",
        bypasses=[],
    )
    assert c.id == "conservative_therapy"
    assert c.weight == 0.30
    assert c.required is True
    assert c.lcd_section == "L34220 §4.2"


def test_policy_criterion_defaults():
    """Required=False, lcd_section=None, bypasses=[] by default."""
    c = PolicyCriterion(id="test", description="Test", weight=0.5)
    assert c.required is False
    assert c.lcd_section is None
    assert c.bypasses == []


def test_policy_definition_valid():
    """Construct PolicyDefinition with criteria list."""
    criteria = [
        PolicyCriterion(id="c1", description="Criterion 1", weight=0.6),
        PolicyCriterion(id="c2", description="Criterion 2", weight=0.4),
    ]
    p = PolicyDefinition(
        policy_id="lcd-test",
        policy_name="Test Policy",
        payer="CMS Medicare",
        procedure_codes=["72148"],
        diagnosis_codes=["M54.5"],
        criteria=criteria,
    )
    assert p.policy_id == "lcd-test"
    assert len(p.criteria) == 2
    assert p.lcd_reference is None


def test_policy_criterion_bypasses_field():
    """Verify bypasses list works."""
    c = PolicyCriterion(
        id="red_flag",
        description="Red flag symptoms",
        weight=0.25,
        bypasses=["conservative_therapy_4wk"],
    )
    assert c.bypasses == ["conservative_therapy_4wk"]


def test_policy_definition_with_lcd_metadata():
    """PolicyDefinition with LCD metadata fields populated."""
    p = PolicyDefinition(
        policy_id="lcd-mri-lumbar-L34220",
        policy_name="MRI Lumbar Spine",
        lcd_reference="L34220",
        lcd_title="Lumbar MRI",
        lcd_contractor="Noridian Healthcare Solutions",
        payer="CMS Medicare",
        procedure_codes=["72148", "72149"],
        diagnosis_codes=["M54.5"],
        criteria=[],
    )
    assert p.lcd_reference == "L34220"
    assert p.lcd_title == "Lumbar MRI"
    assert p.lcd_contractor == "Noridian Healthcare Solutions"
