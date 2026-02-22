"""Policy registry for resolving procedure codes to policy definitions."""

from src.models.policy import PolicyDefinition
from src.policies.generic_policy import build_generic_policy


class PolicyRegistry:
    """Resolves procedure codes to LCD-backed policy definitions."""

    def __init__(self) -> None:
        self._by_cpt: dict[str, PolicyDefinition] = {}

    def register(self, policy: PolicyDefinition) -> None:
        for cpt in policy.procedure_codes:
            self._by_cpt[cpt] = policy

    def resolve(self, procedure_code: str) -> PolicyDefinition:
        """Return LCD-backed policy if available, else generic fallback."""
        if procedure_code in self._by_cpt:
            return self._by_cpt[procedure_code]
        return build_generic_policy(procedure_code)


# Module-level singleton
registry = PolicyRegistry()

# Import seed policies to register them
from src.policies.seed import register_all_seeds  # noqa: E402

register_all_seeds(registry)
