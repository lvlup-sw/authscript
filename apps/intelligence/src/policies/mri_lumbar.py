"""MRI Lumbar Spine policy definition for Blue Cross."""

from typing import Any

# MRI Lumbar Spine - Blue Cross Prior Authorization Policy
# This is a hardcoded policy definition for the demo
MRI_LUMBAR_POLICY: dict[str, Any] = {
    "policy_id": "bcbs-mri-lumbar-2024",
    "policy_name": "MRI Lumbar Spine Prior Authorization",
    "payer": "Blue Cross Blue Shield",
    "procedure_codes": ["72148", "72149", "72158"],  # CPT codes for lumbar MRI
    "diagnosis_codes": {
        "primary": [
            "M54.5",   # Low back pain
            "M54.50",  # Low back pain, site unspecified
            "M54.51",  # Vertebrogenic low back pain
            "M54.52",  # Low back pain due to muscle strain
        ],
        "supporting": [
            "M51.16",  # Intervertebral disc disorders with radiculopathy, lumbar
            "M51.17",  # Intervertebral disc disorders with radiculopathy, lumbosacral
            "G89.29",  # Other chronic pain
            "M47.816", # Spondylosis without myelopathy, lumbar region
            "M47.817", # Spondylosis without myelopathy, lumbosacral region
        ],
    },
    "criteria": [
        {
            "id": "conservative_therapy",
            "description": "6+ weeks of conservative therapy (physical therapy, medication, etc.)",
            "evidence_patterns": [
                r"physical therapy.*(\d+)\s*weeks",
                r"PT\s*x\s*(\d+)\s*weeks",
                r"conservative.*treatment.*(\d+)\s*weeks",
                r"(\d+)\s*weeks.*physical therapy",
                r"NSAIDs?\s+for\s+(\d+)\s*weeks",
            ],
            "threshold_weeks": 6,
            "required": True,
        },
        {
            "id": "failed_treatment",
            "description": "Documentation of treatment failure or inadequate response",
            "evidence_patterns": [
                r"failed|inadequate|no improvement|persistent",
                r"continue.*to.*experience.*pain",
                r"symptoms.*not.*resolved",
                r"unresponsive to.*treatment",
                r"refractory",
            ],
            "required": True,
        },
        {
            "id": "neurological_symptoms",
            "description": (
                "Red flag neurological symptoms "
                "(bypasses conservative therapy requirement)"
            ),
            "evidence_patterns": [
                r"radiculopathy",
                r"weakness|numbness|tingling",
                r"bowel|bladder.*dysfunction",
                r"cauda equina",
                r"progressive.*neurological",
                r"motor deficit",
                r"sensory deficit",
            ],
            "bypasses_conservative": True,
            "required": False,
        },
        {
            "id": "diagnosis_present",
            "description": "Valid ICD-10 diagnosis code present",
            "required": True,
        },
    ],
    "form_field_mappings": {
        "patient_name": "PatientName",
        "patient_dob": "PatientDOB",
        "member_id": "MemberID",
        "diagnosis_primary": "PrimaryDiagnosis",
        "diagnosis_secondary": "SecondaryDiagnosis",
        "procedure_code": "ProcedureCode",
        "clinical_summary": "ClinicalJustification",
        "provider_name": "OrderingProviderName",
        "provider_npi": "OrderingProviderNPI",
        "facility_name": "FacilityName",
        "date_of_service": "RequestedDateOfService",
    },
}
