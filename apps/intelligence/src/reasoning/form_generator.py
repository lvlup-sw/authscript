"""STUB: Generate PA form data from extracted evidence.

Production implementation will calculate recommendations based on
evidence and generate clinical summaries using LLM.
"""

from typing import Any

from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem, PAFormResponse


async def generate_form_data(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> PAFormResponse:
    """
    STUB: Return APPROVE recommendation with high confidence.

    Production implementation will:
    1. Calculate recommendation based on evidence (APPROVE/NEED_INFO/MANUAL_REVIEW)
    2. Generate clinical summary via LLM or template
    3. Build PDF field mappings from policy configuration

    Args:
        clinical_bundle: FHIR clinical data bundle
        evidence: Extracted evidence items
        policy: Policy definition with field mappings

    Returns:
        Complete PA form response ready for PDF stamping
    """
    patient_name = "Unknown"
    patient_dob = "Unknown"
    member_id = "Unknown"

    if clinical_bundle.patient:
        patient_name = clinical_bundle.patient.name
        if clinical_bundle.patient.birth_date:
            patient_dob = clinical_bundle.patient.birth_date.isoformat()
        if clinical_bundle.patient.member_id:
            member_id = clinical_bundle.patient.member_id

    diagnosis_codes = [c.code for c in clinical_bundle.conditions]
    if not diagnosis_codes:
        diagnosis_codes = ["Unknown"]

    procedure_codes = policy.get("procedure_codes") or ["72148"]
    procedure_code = procedure_codes[0]

    return PAFormResponse(
        patient_name=patient_name,
        patient_dob=patient_dob,
        member_id=member_id,
        diagnosis_codes=diagnosis_codes,
        procedure_code=procedure_code,
        clinical_summary="STUB: Clinical summary would be generated from evidence.",
        supporting_evidence=evidence,
        recommendation="APPROVE",
        confidence_score=0.95,
        field_mappings={
            "PatientName": patient_name,
            "PatientDOB": patient_dob,
            "MemberID": member_id,
            "PrimaryDiagnosis": diagnosis_codes[0] if diagnosis_codes else "Unknown",
            "ProcedureCode": procedure_code,
            "ClinicalJustification": "STUB: Clinical justification",
        },
    )
