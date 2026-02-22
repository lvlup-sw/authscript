"""Seed policy loader."""
from src.policies.seed.mri_lumbar import POLICY as MRI_LUMBAR
from src.policies.seed.mri_brain import POLICY as MRI_BRAIN
from src.policies.seed.tka import POLICY as TKA
from src.policies.seed.physical_therapy import POLICY as PHYSICAL_THERAPY
from src.policies.seed.epidural_steroid import POLICY as EPIDURAL_STEROID

ALL_SEED_POLICIES = [MRI_LUMBAR, MRI_BRAIN, TKA, PHYSICAL_THERAPY, EPIDURAL_STEROID]


def register_all_seeds(registry) -> None:
    for policy in ALL_SEED_POLICIES:
        registry.register(policy)
