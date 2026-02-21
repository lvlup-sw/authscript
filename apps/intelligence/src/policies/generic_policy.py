"""Generic policy builder for unsupported procedure codes.

When a procedure code is not in the specific policy set, we fall back to
a generic policy that uses broad criteria the LLM can evaluate against
any clinical documentation.
"""

from typing import Any


def build_generic_policy(procedure_code: str) -> dict[str, Any]:
    """Build a generic prior authorization policy for any procedure code.

    The generic policy uses broad criteria (medical necessity, diagnosis
    validation, clinical documentation) that the LLM can evaluate against
    any clinical data, regardless of procedure type.
    """
    return {
        "policy_id": f"generic-{procedure_code}",
        "policy_name": f"Generic Prior Authorization - {procedure_code}",
        "payer": "Generic",
        "procedure_codes": [procedure_code],
        "diagnosis_codes": {
            "primary": [],
            "supporting": [],
        },
        "criteria": [
            {
                "id": "medical_necessity",
                "description": "The requested procedure is medically necessary based on the clinical documentation",
                "evidence_patterns": [
                    r"medically necessary",
                    r"medical necessity",
                    r"clinically indicated",
                    r"recommended.*procedure",
                    r"required.*treatment",
                ],
                "required": True,
            },
            {
                "id": "diagnosis_present",
                "description": "Valid ICD-10 diagnosis code present supporting the procedure",
                "evidence_patterns": [
                    r"[A-Z]\d{2}\.?\d{0,4}",
                    r"diagnosis",
                    r"diagnosed with",
                ],
                "required": True,
            },
            {
                "id": "clinical_documentation",
                "description": "Sufficient clinical documentation supports the request",
                "evidence_patterns": [
                    r"clinical.*documentation",
                    r"medical records",
                    r"chart.*notes",
                    r"history.*physical",
                    r"assessment.*plan",
                ],
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
