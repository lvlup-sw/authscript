"""Total Knee Arthroplasty seed policy — LCD L36575."""

from src.models.policy import PolicyCriterion, PolicyDefinition

POLICY = PolicyDefinition(
    policy_id="lcd-tka-L36575",
    policy_name="Total Knee Arthroplasty",
    lcd_reference="L36575",
    lcd_title="Total Knee Arthroplasty",
    lcd_contractor="Noridian Healthcare Solutions",
    payer="CMS Medicare",
    procedure_codes=["27447"],
    diagnosis_codes=["M17.0", "M17.11", "M17.12", "M87.052"],
    criteria=[
        PolicyCriterion(
            id="diagnosis_present",
            description="Valid ICD-10 for knee joint disease",
            weight=0.10,
            required=True,
            lcd_section="L36575 / A57685 — Covered Diagnoses",
        ),
        PolicyCriterion(
            id="advanced_joint_disease",
            description="Imaging showing joint space narrowing, osteophytes, sclerosis, AVN",
            weight=0.25,
            required=True,
            lcd_section="L36575 — Advanced Joint Disease",
        ),
        PolicyCriterion(
            id="functional_impairment",
            description="Pain/disability interfering with ADLs, increased with weight bearing",
            weight=0.25,
            required=True,
            lcd_section="L36575 — Functional Impairment",
        ),
        PolicyCriterion(
            id="failed_conservative_mgmt",
            description="Documented trials of NSAIDs, PT, assistive devices, injections",
            weight=0.30,
            required=True,
            lcd_section="L36575 — Failed Conservative Management",
        ),
        PolicyCriterion(
            id="no_contraindication",
            description="No active joint infection, systemic bacteremia, skin infection at site",
            weight=0.10,
            required=True,
            lcd_section="L36575 — Contraindications",
        ),
    ],
)
