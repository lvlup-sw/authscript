"""MRI Brain seed policy — LCD L37373."""

from src.models.policy import PolicyCriterion, PolicyDefinition

POLICY = PolicyDefinition(
    policy_id="lcd-mri-brain-L37373",
    policy_name="MRI Brain",
    lcd_reference="L37373",
    lcd_title="Magnetic Resonance Imaging of the Brain",
    lcd_contractor="Noridian Healthcare Solutions",
    payer="CMS Medicare",
    procedure_codes=["70551", "70552", "70553"],
    diagnosis_codes=["G40.909", "R51.9", "G43.909", "G35"],
    criteria=[
        PolicyCriterion(
            id="diagnosis_present",
            description="Valid ICD-10 for neurological condition",
            weight=0.15,
            required=True,
            lcd_section="L37373 / A57204 — Covered Diagnoses",
        ),
        PolicyCriterion(
            id="neurological_indication",
            description="Tumor, stroke, MS, seizures, unexplained neuro deficit",
            weight=0.35,
            required=True,
            lcd_section="L37373 — Indications for MRI",
        ),
        PolicyCriterion(
            id="ct_insufficient",
            description="CT already performed and insufficient, or MRI specifically indicated",
            weight=0.25,
            required=False,
            lcd_section="L37373 — MRI vs CT Selection",
        ),
        PolicyCriterion(
            id="clinical_documentation",
            description="Supporting clinical findings documented",
            weight=0.25,
            required=True,
            lcd_section="L37373 — Coverage Requirements",
        ),
    ],
)
