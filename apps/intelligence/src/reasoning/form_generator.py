"""Generate PA form data from extracted evidence using LLM.

Calculates recommendations and generates clinical summaries.
"""

from src.llm_client import chat_completion
from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem, PAFormResponse
from src.models.policy import PolicyDefinition
from src.reasoning.confidence_scorer import calculate_confidence


async def generate_form_data(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: PolicyDefinition,
) -> PAFormResponse:
    """
    Generate PA form data from extracted evidence using LLM.

    Delegates scoring to confidence_scorer and generates clinical summary.

    Args:
        clinical_bundle: FHIR clinical data bundle
        evidence: Extracted evidence items
        policy: PolicyDefinition with criteria and metadata

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

    procedure_code = policy.procedure_codes[0] if policy.procedure_codes else "72148"

    # Delegate scoring to confidence_scorer
    score_result = calculate_confidence(evidence, policy)

    # Generate clinical summary using LLM
    evidence_summary = "\n".join(
        [f"- {e.criterion_id}: {e.status} - {e.evidence[:100]}" for e in evidence]
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

Patient: [REDACTED]
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

    field_mappings = {
        "PatientName": patient_name,
        "PatientDOB": patient_dob,
        "MemberID": member_id,
        "PrimaryDiagnosis": diagnosis_codes[0] if diagnosis_codes else "Unknown",
        "ProcedureCode": procedure_code,
        "ClinicalJustification": clinical_summary,
    }

    return PAFormResponse(
        patient_name=patient_name,
        patient_dob=patient_dob,
        member_id=member_id,
        diagnosis_codes=diagnosis_codes,
        procedure_code=procedure_code,
        clinical_summary=clinical_summary,
        supporting_evidence=evidence,
        recommendation=score_result.recommendation,
        confidence_score=score_result.score,
        field_mappings=field_mappings,
        policy_id=policy.policy_id,
        lcd_reference=policy.lcd_reference,
    )
