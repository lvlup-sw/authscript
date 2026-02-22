"""Epidural Steroid Injection seed policy — LCD L39240."""

from src.models.policy import PolicyCriterion, PolicyDefinition

POLICY = PolicyDefinition(
    policy_id="lcd-esi-L39240",
    policy_name="Epidural Steroid Injection",
    lcd_reference="L39240",
    lcd_title="Epidural Steroid Injections",
    lcd_contractor="Noridian Healthcare Solutions",
    payer="CMS Medicare",
    procedure_codes=["62322", "62323"],
    diagnosis_codes=["M54.10", "M54.16", "M54.17", "M48.06"],
    criteria=[
        PolicyCriterion(
            id="diagnosis_confirmed",
            description="Radiculopathy/stenosis confirmed by history, exam, and imaging",
            weight=0.25,
            required=True,
            lcd_section="L39240 — Requirement 1",
        ),
        PolicyCriterion(
            id="severity_documented",
            description="Pain severe enough to impact QoL/function, documented with standardized scale",
            weight=0.20,
            required=True,
            lcd_section="L39240 — Requirement 2",
        ),
        PolicyCriterion(
            id="conservative_care_4wk",
            description="4 weeks conservative care failed/intolerable (except acute herpes zoster)",
            weight=0.25,
            required=True,
            lcd_section="L39240 — Requirement 3",
        ),
        PolicyCriterion(
            id="frequency_within_limits",
            description="<=4 sessions per region per rolling 12 months",
            weight=0.15,
            required=True,
            lcd_section="L39240 — Frequency Limits",
        ),
        PolicyCriterion(
            id="image_guidance_planned",
            description="Fluoroscopy or CT guidance with contrast planned",
            weight=0.15,
            required=True,
            lcd_section="L39240 — Procedural Requirements",
        ),
    ],
)
