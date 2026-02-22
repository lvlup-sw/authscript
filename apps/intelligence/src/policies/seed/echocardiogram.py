"""Echocardiogram seed policy — ACC/AHA Appropriate Use Criteria."""

from src.models.policy import PolicyCriterion, PolicyDefinition

POLICY = PolicyDefinition(
    policy_id="auc-echocardiogram",
    policy_name="Transthoracic Echocardiogram",
    lcd_reference=None,
    lcd_title="Transthoracic Echocardiography (TTE)",
    lcd_contractor=None,
    payer="CMS Medicare",
    procedure_codes=["93303", "93304", "93306", "93307", "93308"],
    diagnosis_codes=[
        "I50.32", "I50.22", "I50.42",  # Heart failure (systolic, diastolic, combined)
        "I48.91", "I48.0", "I48.1",     # Atrial fibrillation / flutter
        "I35.0", "I34.0", "I06.0",      # Valvular disease (aortic, mitral, rheumatic)
        "I42.0", "I42.9",               # Cardiomyopathy
        "I25.10",                        # CAD
    ],
    criteria=[
        PolicyCriterion(
            id="diagnosis_present",
            description="Valid ICD-10 for cardiac pathology (heart failure, valvular disease, arrhythmia, cardiomyopathy)",
            weight=0.20,
            required=True,
            lcd_section="ACC/AHA AUC — Covered Cardiac Diagnoses",
        ),
        PolicyCriterion(
            id="clinical_indication",
            description="Documented clinical indication for echocardiographic assessment (evaluate EF, assess valvular function, monitor known condition, new symptoms)",
            weight=0.35,
            required=True,
            lcd_section="ACC/AHA AUC — Clinical Indications for TTE",
        ),
        PolicyCriterion(
            id="symptom_or_change",
            description="New or worsening symptoms, or clinical change warranting imaging (dyspnea, edema, chest pain, syncope, new murmur)",
            weight=0.25,
            required=False,
            lcd_section="ACC/AHA AUC — Symptom-Based Indications",
        ),
        PolicyCriterion(
            id="no_recent_duplicate",
            description="No echocardiogram within prior 12 months for same indication, unless clinical change documented",
            weight=0.20,
            required=False,
            lcd_section="ACC/AHA AUC — Repeat Study Appropriateness",
        ),
    ],
)
