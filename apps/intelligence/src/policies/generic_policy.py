"""Generic fallback policy for unsupported procedure codes."""

from src.models.policy import PolicyCriterion, PolicyDefinition


def build_generic_policy(procedure_code: str) -> PolicyDefinition:
    """Build a generic policy for any procedure code.

    Used as a fallback when no specific LCD policy is registered
    for the given procedure code.

    Args:
        procedure_code: CPT procedure code

    Returns:
        A generic PolicyDefinition with basic criteria
    """
    return PolicyDefinition(
        policy_id=f"generic-{procedure_code}",
        policy_name=f"Generic Policy for {procedure_code}",
        payer="Generic",
        procedure_codes=[procedure_code],
        criteria=[
            PolicyCriterion(
                id="medical_necessity",
                description="Documentation of medical necessity for the requested procedure",
                weight=1.0,
                required=True,
            ),
            PolicyCriterion(
                id="diagnosis_support",
                description="Valid diagnosis code supporting the procedure",
                weight=0.8,
                required=True,
            ),
            PolicyCriterion(
                id="clinical_documentation",
                description="Adequate clinical documentation provided",
                weight=0.6,
            ),
        ],
        lcd_reference=None,
    )
