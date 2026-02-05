"""Generate PA form data from extracted evidence using LLM.

Calculates recommendations and generates clinical summaries.
"""

from typing import Any, Literal

from src.llm_client import chat_completion
from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem, PAFormResponse


async def generate_form_data(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> PAFormResponse:
    """
    Generate PA form data from extracted evidence using LLM.

    Calculates recommendation based on evidence and generates clinical summary.

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

    # Calculate recommendation based on evidence
    required_criteria = [
        c for c in policy.get("criteria", []) if c.get("required", False)
    ]
    required_criterion_ids = {c.get("id") for c in required_criteria}

    met_required = all(
        e.status == "MET"
        for e in evidence
        if e.criterion_id in required_criterion_ids
    )

    has_not_met = any(e.status == "NOT_MET" for e in evidence)

    recommendation: Literal["APPROVE", "NEED_INFO", "MANUAL_REVIEW"]
    if met_required and not has_not_met:
        recommendation = "APPROVE"
        confidence_score = 0.9
    elif has_not_met:
        recommendation = "NEED_INFO"
        confidence_score = 0.6
    else:
        recommendation = "MANUAL_REVIEW"
        confidence_score = 0.7

    # Generate clinical summary using LLM
    evidence_summary = "\n".join(
        [
            f"- {e.criterion_id}: {e.status} - {e.evidence[:100]}"
            for e in evidence
        ]
    )

    system_prompt = (
        "You are a medical prior authorization specialist. Generate a concise "
        "clinical summary for prior authorization."
    )
    user_prompt = f"""
Based on the following evidence evaluation, generate a brief clinical summary
(2-3 sentences) explaining the medical necessity for this procedure.

Evidence Summary:
{evidence_summary}

Patient: {patient_name}
Diagnoses: {', '.join(diagnosis_codes)}
Procedure: {procedure_code}

Generate a professional clinical summary.
"""

    clinical_summary = await chat_completion(
        system_prompt=system_prompt,
        user_prompt=user_prompt,
        temperature=0.5,
        max_tokens=1000,
    ) or "Clinical summary generation pending."

    # Build field mappings
    field_mappings = {
        "PatientName": patient_name,
        "PatientDOB": patient_dob,
        "MemberID": member_id,
        "PrimaryDiagnosis": diagnosis_codes[0] if diagnosis_codes else "Unknown",
        "ProcedureCode": procedure_code,
        "ClinicalJustification": clinical_summary,
    }

    # Add policy-defined field mappings
    policy_mappings = policy.get("form_field_mappings", {})
    for key, value in policy_mappings.items():
        if key in field_mappings:
            field_mappings[value] = field_mappings[key]

    return PAFormResponse(
        patient_name=patient_name,
        patient_dob=patient_dob,
        member_id=member_id,
        diagnosis_codes=diagnosis_codes,
        procedure_code=procedure_code,
        clinical_summary=clinical_summary,
        supporting_evidence=evidence,
        recommendation=recommendation,
        confidence_score=confidence_score,
        field_mappings=field_mappings,
    )
