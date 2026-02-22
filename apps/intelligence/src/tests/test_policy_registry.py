"""Tests for policy registry."""
import pytest
from src.models.policy import PolicyDefinition
from src.policies.registry import PolicyRegistry, registry


def test_register_and_resolve_known_cpt():
    """Register policy with CPT, resolve returns same policy."""
    r = PolicyRegistry()
    policy = PolicyDefinition(
        policy_id="test", policy_name="Test", payer="Test",
        procedure_codes=["72148"], criteria=[]
    )
    r.register(policy)
    assert r.resolve("72148") is policy


def test_resolve_unknown_cpt_returns_generic():
    """Unregistered CPT returns generic fallback."""
    r = PolicyRegistry()
    result = r.resolve("99999")
    assert result.lcd_reference is None
    assert "99999" in result.procedure_codes


def test_register_multi_cpt_policy():
    """Policy with 3 CPTs, all 3 resolve to it."""
    r = PolicyRegistry()
    policy = PolicyDefinition(
        policy_id="multi", policy_name="Multi", payer="Test",
        procedure_codes=["72148", "72149", "72158"], criteria=[]
    )
    r.register(policy)
    assert r.resolve("72148") is policy
    assert r.resolve("72149") is policy
    assert r.resolve("72158") is policy


def test_seed_policies_registered_on_import():
    """Module-level registry has pre-registered seed policies."""
    # 72148 is MRI Lumbar CPT
    result = registry.resolve("72148")
    assert result.lcd_reference is not None


def test_all_seed_cpts_resolve_to_lcd_policy():
    """All 14 seed CPT codes resolve to LCD-backed policies."""
    seed_cpts = [
        "72148", "72149", "72158",  # MRI Lumbar
        "70551", "70552", "70553",  # MRI Brain
        "27447",                    # TKA
        "97161", "97162", "97163",  # Physical Therapy
        "62322", "62323",           # Epidural Steroid
    ]
    for cpt in seed_cpts:
        result = registry.resolve(cpt)
        assert result.lcd_reference is not None, f"CPT {cpt} should have LCD reference"


def test_seed_policy_lcd_references_populated():
    """All seed policies have non-null lcd_reference."""
    seed_cpts = ["72148", "70551", "27447", "97161", "62322"]
    for cpt in seed_cpts:
        result = registry.resolve(cpt)
        assert result.lcd_reference is not None


def test_seed_policy_weights_sum_approximately_one():
    """All seed policies have weights summing to ~1.0."""
    seed_cpts = ["72148", "70551", "27447", "97161", "62322"]
    for cpt in seed_cpts:
        policy = registry.resolve(cpt)
        total = sum(c.weight for c in policy.criteria)
        assert total == pytest.approx(1.0, abs=0.01), f"Policy {policy.policy_id}: weights sum to {total}"
