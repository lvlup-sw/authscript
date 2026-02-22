"""Generic fallback policy for unsupported procedure codes."""

from src.models.policy import PolicyCriterion, PolicyDefinition


def build_generic_policy(procedure_code: str) -> PolicyDefinition:
    """Build a generic medical necessity policy for any procedure code."""
    return PolicyDefinition(
        policy_id=f"generic-{procedure_code}",
        policy_name="General Medical Necessity",
        lcd_reference=None,
        payer="General",
        procedure_codes=[procedure_code],
        diagnosis_codes=[],
        criteria=[
            PolicyCriterion(
                id="medical_necessity",
                description="Medical necessity is documented with clinical rationale",
                weight=0.40,
                required=True,
            ),
            PolicyCriterion(
                id="diagnosis_present",
                description="Valid diagnosis code is present and supports the procedure",
                weight=0.30,
                required=True,
            ),
            PolicyCriterion(
                id="conservative_therapy",
                description="Conservative therapy attempted or documented as not applicable",
                weight=0.30,
                required=False,
            ),
        ],
    )
