"""Policy registry for resolving procedure codes to policy definitions."""

from src.models.policy import PolicyCriterion, PolicyDefinition
from src.policies.generic_policy import build_generic_policy


class PolicyRegistry:
    """Registry mapping procedure codes to PolicyDefinition instances.

    Falls back to a generic policy when no specific policy is registered.
    """

    def __init__(self) -> None:
        self._policies: dict[str, PolicyDefinition] = {}

    def register(self, policy: PolicyDefinition) -> None:
        """Register a policy for all its procedure codes."""
        for code in policy.procedure_codes:
            self._policies[code] = policy

    def resolve(self, procedure_code: str) -> PolicyDefinition:
        """Resolve a procedure code to a PolicyDefinition.

        Returns the registered policy if one exists, otherwise
        builds a generic fallback policy.

        Args:
            procedure_code: CPT procedure code

        Returns:
            PolicyDefinition for the procedure code
        """
        if procedure_code in self._policies:
            return self._policies[procedure_code]
        return build_generic_policy(procedure_code)

    @property
    def registered_codes(self) -> set[str]:
        """Return all registered procedure codes."""
        return set(self._policies.keys())


def _build_seed_policies() -> list[PolicyDefinition]:
    """Build the initial set of seed policies."""
    return [
        # MRI Lumbar Spine LCD
        PolicyDefinition(
            policy_id="lcd-mri-lumbar-L34220",
            policy_name="MRI Lumbar Spine Prior Authorization (LCD L34220)",
            payer="CMS",
            procedure_codes=["72148", "72149", "72158"],
            lcd_reference="L34220",
            criteria=[
                PolicyCriterion(
                    id="conservative_therapy",
                    description="6+ weeks of conservative therapy (physical therapy, medication, etc.)",
                    weight=1.0,
                    required=True,
                ),
                PolicyCriterion(
                    id="failed_treatment",
                    description="Documentation of treatment failure or inadequate response",
                    weight=0.9,
                    required=True,
                ),
                PolicyCriterion(
                    id="neurological_symptoms",
                    description="Red flag neurological symptoms (radiculopathy, weakness, numbness)",
                    weight=0.8,
                ),
                PolicyCriterion(
                    id="diagnosis_present",
                    description="Valid ICD-10 diagnosis code present",
                    weight=0.7,
                    required=True,
                ),
                PolicyCriterion(
                    id="prior_imaging",
                    description="Prior imaging results reviewed or documented",
                    weight=0.5,
                ),
            ],
        ),
        # MRI Cervical Spine LCD
        PolicyDefinition(
            policy_id="lcd-mri-cervical-L34221",
            policy_name="MRI Cervical Spine Prior Authorization (LCD L34221)",
            payer="CMS",
            procedure_codes=["72141", "72142", "72156"],
            lcd_reference="L34221",
            criteria=[
                PolicyCriterion(
                    id="conservative_therapy",
                    description="4+ weeks of conservative therapy for cervical spine",
                    weight=1.0,
                    required=True,
                ),
                PolicyCriterion(
                    id="neurological_symptoms",
                    description="Cervical radiculopathy or myelopathy symptoms",
                    weight=0.9,
                ),
                PolicyCriterion(
                    id="diagnosis_present",
                    description="Valid ICD-10 diagnosis code for cervical condition",
                    weight=0.7,
                    required=True,
                ),
            ],
        ),
        # CT Abdomen/Pelvis LCD
        PolicyDefinition(
            policy_id="lcd-ct-abdomen-L34500",
            policy_name="CT Abdomen/Pelvis Prior Authorization (LCD L34500)",
            payer="CMS",
            procedure_codes=["74176", "74177", "74178"],
            lcd_reference="L34500",
            criteria=[
                PolicyCriterion(
                    id="clinical_indication",
                    description="Clinical indication for abdominal imaging",
                    weight=1.0,
                    required=True,
                ),
                PolicyCriterion(
                    id="prior_workup",
                    description="Prior non-imaging workup completed",
                    weight=0.8,
                ),
                PolicyCriterion(
                    id="diagnosis_present",
                    description="Valid ICD-10 diagnosis code present",
                    weight=0.7,
                    required=True,
                ),
            ],
        ),
        # Knee MRI LCD
        PolicyDefinition(
            policy_id="lcd-mri-knee-L34600",
            policy_name="MRI Knee Prior Authorization (LCD L34600)",
            payer="CMS",
            procedure_codes=["73721", "73722", "73723"],
            lcd_reference="L34600",
            criteria=[
                PolicyCriterion(
                    id="conservative_therapy",
                    description="4+ weeks of conservative therapy for knee",
                    weight=1.0,
                    required=True,
                ),
                PolicyCriterion(
                    id="clinical_exam",
                    description="Physical examination findings documented",
                    weight=0.8,
                    required=True,
                ),
                PolicyCriterion(
                    id="diagnosis_present",
                    description="Valid ICD-10 diagnosis code for knee condition",
                    weight=0.7,
                    required=True,
                ),
            ],
        ),
        # Shoulder MRI LCD
        PolicyDefinition(
            policy_id="lcd-mri-shoulder-L34700",
            policy_name="MRI Shoulder Prior Authorization (LCD L34700)",
            payer="CMS",
            procedure_codes=["73221", "73222", "73223"],
            lcd_reference="L34700",
            criteria=[
                PolicyCriterion(
                    id="conservative_therapy",
                    description="6+ weeks of conservative therapy for shoulder",
                    weight=1.0,
                    required=True,
                ),
                PolicyCriterion(
                    id="clinical_exam",
                    description="Physical examination of shoulder documented",
                    weight=0.8,
                    required=True,
                ),
                PolicyCriterion(
                    id="diagnosis_present",
                    description="Valid ICD-10 diagnosis code for shoulder condition",
                    weight=0.7,
                    required=True,
                ),
            ],
        ),
    ]


# Singleton registry with seed policies pre-registered
registry = PolicyRegistry()
for _policy in _build_seed_policies():
    registry.register(_policy)
