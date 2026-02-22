"""MRI Lumbar Spine seed policy — LCD L34220."""

from src.models.policy import PolicyCriterion, PolicyDefinition

POLICY = PolicyDefinition(
    policy_id="lcd-mri-lumbar-L34220",
    policy_name="MRI Lumbar Spine",
    lcd_reference="L34220",
    lcd_title="Magnetic Resonance Imaging of the Lumbar Spine",
    lcd_contractor="Noridian Healthcare Solutions",
    payer="CMS Medicare",
    procedure_codes=["72148", "72149", "72158"],
    diagnosis_codes=["M54.5", "M54.50", "M54.51", "M51.16", "M51.17"],
    criteria=[
        PolicyCriterion(
            id="diagnosis_present",
            description="Valid ICD-10 for lumbar pathology",
            weight=0.15,
            required=True,
            lcd_section="L34220 / A57206 — Covered Diagnoses",
        ),
        PolicyCriterion(
            id="red_flag_screening",
            description="Cauda equina, tumor, infection, major neuro deficit",
            weight=0.25,
            required=False,
            lcd_section="L34220 — Immediate MRI Indications",
            bypasses=["conservative_therapy_4wk"],
        ),
        PolicyCriterion(
            id="conservative_therapy_4wk",
            description="4+ weeks conservative management (NSAIDs, PT, activity mod) documented",
            weight=0.30,
            required=True,
            lcd_section="L34220 — Non-Red-Flag Requirements",
        ),
        PolicyCriterion(
            id="clinical_rationale",
            description="Imaging abnormalities alone insufficient; supporting clinical rationale documented",
            weight=0.20,
            required=True,
            lcd_section="L34220 — Coverage Principle",
        ),
        PolicyCriterion(
            id="no_duplicate_imaging",
            description="No recent duplicative CT/MRI without new justification",
            weight=0.10,
            required=False,
            lcd_section="L34220 — Non-Covered Indications",
        ),
    ],
)
