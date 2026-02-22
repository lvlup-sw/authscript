"""Physical Therapy seed policy — LCD L34049."""

from src.models.policy import PolicyCriterion, PolicyDefinition

POLICY = PolicyDefinition(
    policy_id="lcd-pt-L34049",
    policy_name="Physical Therapy",
    lcd_reference="L34049",
    lcd_title="Outpatient Physical and Occupational Therapy Services",
    lcd_contractor="Noridian Healthcare Solutions",
    payer="CMS Medicare",
    procedure_codes=["97161", "97162", "97163"],
    diagnosis_codes=["M54.5", "M25.561", "M79.3", "S83.511A"],
    criteria=[
        PolicyCriterion(
            id="improvement_potential",
            description="Patient condition has improvement potential or actively improving",
            weight=0.30,
            required=True,
            lcd_section="L34049 — Rehabilitative Therapy",
        ),
        PolicyCriterion(
            id="skilled_service_required",
            description="Service requires professional judgment, cannot be self-administered",
            weight=0.25,
            required=True,
            lcd_section="L34049 — Skilled Service Requirements",
        ),
        PolicyCriterion(
            id="individualized_plan",
            description="Plan of care with goals, frequency, duration documented",
            weight=0.25,
            required=True,
            lcd_section="L34049 — Documentation Requirements",
        ),
        PolicyCriterion(
            id="objective_progress",
            description="Successive objective measurements demonstrate progress",
            weight=0.20,
            required=False,
            lcd_section="L34049 — Progress Documentation",
        ),
    ],
)
