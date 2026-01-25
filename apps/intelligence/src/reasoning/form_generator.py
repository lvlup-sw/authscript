"""Generate PA form data from extracted evidence."""

from datetime import date
from typing import Any, Literal

from src.config import settings
from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem, PAFormResponse


async def generate_form_data(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> PAFormResponse:
    """
    Generate complete PA form response from clinical data and evidence.
    """
    # Calculate recommendation based on evidence
    recommendation, confidence = _calculate_recommendation(evidence, policy)

    # Extract patient info
    patient_name = "Unknown"
    patient_dob = "Unknown"
    member_id = "Unknown"

    if clinical_bundle.patient:
        patient_name = clinical_bundle.patient.name
        if clinical_bundle.patient.birth_date:
            patient_dob = clinical_bundle.patient.birth_date.isoformat()
        if clinical_bundle.patient.member_id:
            member_id = clinical_bundle.patient.member_id

    # Get diagnosis codes
    diagnosis_codes = [c.code for c in clinical_bundle.conditions]
    if not diagnosis_codes:
        diagnosis_codes = ["Unknown"]

    # Generate clinical summary
    clinical_summary = await _generate_clinical_summary(
        clinical_bundle, evidence, policy
    )

    # Build field mappings for PDF form
    field_mappings = _build_field_mappings(
        patient_name,
        patient_dob,
        member_id,
        diagnosis_codes,
        policy.get("procedure_codes", ["72148"])[0],
        clinical_summary,
        policy,
    )

    return PAFormResponse(
        patient_name=patient_name,
        patient_dob=patient_dob,
        member_id=member_id,
        diagnosis_codes=diagnosis_codes,
        procedure_code=policy.get("procedure_codes", ["72148"])[0],
        clinical_summary=clinical_summary,
        supporting_evidence=evidence,
        recommendation=recommendation,
        confidence_score=confidence,
        field_mappings=field_mappings,
    )


Recommendation = Literal["APPROVE", "NEED_INFO", "MANUAL_REVIEW"]


def _calculate_recommendation(
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> tuple[Recommendation, float]:
    """Calculate recommendation and confidence from evidence."""
    criteria = policy.get("criteria", [])
    required_criteria = [c for c in criteria if c.get("required", False)]

    # Check for neurological red flags (bypasses conservative therapy)
    has_red_flags = False
    for item in evidence:
        if item.criterion_id == "neurological_symptoms" and item.status == "MET":
            has_red_flags = True
            break

    # Count met required criteria
    met_required = 0
    total_confidence = 0.0

    for criterion in required_criteria:
        criterion_id = criterion["id"]

        # Skip conservative therapy if red flags present
        if criterion_id == "conservative_therapy" and has_red_flags:
            met_required += 1
            total_confidence += 0.9
            continue

        # Find evidence for this criterion
        for item in evidence:
            if item.criterion_id == criterion_id:
                if item.status == "MET":
                    met_required += 1
                total_confidence += item.confidence
                break

    # Calculate overall confidence
    num_required = len(required_criteria)
    if num_required > 0:
        avg_confidence = total_confidence / num_required
        met_ratio = met_required / num_required
    else:
        avg_confidence = 0.5
        met_ratio = 0.0

    # Determine recommendation
    if met_ratio == 1.0 and avg_confidence >= 0.8:
        return "APPROVE", avg_confidence
    elif met_ratio >= 0.5:
        return "MANUAL_REVIEW", avg_confidence * 0.8
    else:
        return "NEED_INFO", avg_confidence * 0.6


async def _generate_clinical_summary(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> str:
    """Generate a clinical summary for the PA form."""
    # Try LLM generation first
    if settings.llm_configured:
        return await _generate_summary_with_llm(clinical_bundle, evidence, policy)

    # Fallback to template-based summary
    return _generate_template_summary(clinical_bundle, evidence, policy)


async def _generate_summary_with_llm(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> str:
    """Generate clinical summary using LLM."""
    try:
        from src.llm_client import chat_completion

        # Build evidence summary
        evidence_text = "\n".join(
            f"- {item.criterion_id}: {item.status} - {item.evidence}"
            for item in evidence
        )

        system_prompt = (
            "You are a clinical documentation specialist "
            "writing prior authorization justifications."
        )
        user_prompt = f"""Write a 2-3 sentence clinical justification for medical necessity \
of an MRI Lumbar Spine.

Patient Information:
- Name: {clinical_bundle.patient.name if clinical_bundle.patient else 'Unknown'}
- Conditions: {', '.join(c.display or c.code for c in clinical_bundle.conditions)}

Evidence Found:
{evidence_text}

Write a professional medical necessity statement suitable for a prior authorization form.
Focus on the clinical need and supporting evidence. Be concise."""

        content = await chat_completion(
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            temperature=0.3,
            max_tokens=200,
        )

        return content or _generate_template_summary(clinical_bundle, evidence, policy)

    except Exception:
        return _generate_template_summary(clinical_bundle, evidence, policy)


def _generate_template_summary(
    clinical_bundle: ClinicalBundle,
    evidence: list[EvidenceItem],
    policy: dict[str, Any],
) -> str:
    """Generate a template-based clinical summary."""
    conditions = [c.display or c.code for c in clinical_bundle.conditions]
    condition_text = ", ".join(conditions) if conditions else "lumbar spine condition"

    # Find key evidence
    therapy_evidence = next(
        (e for e in evidence if e.criterion_id == "conservative_therapy" and e.status == "MET"),
        None,
    )
    neuro_evidence = next(
        (e for e in evidence if e.criterion_id == "neurological_symptoms" and e.status == "MET"),
        None,
    )

    if neuro_evidence:
        return (
            f"Patient presents with {condition_text} and neurological symptoms requiring "
            f"urgent MRI evaluation. {neuro_evidence.evidence[:100]}..."
        )
    elif therapy_evidence:
        return (
            f"Patient has {condition_text} with documented failure of conservative "
            f"therapy. MRI is medically necessary to evaluate for structural abnormalities "
            f"and guide further treatment."
        )
    else:
        return (
            f"Patient presents with {condition_text}. MRI Lumbar Spine is requested "
            f"for diagnostic evaluation and treatment planning."
        )


def _build_field_mappings(
    patient_name: str,
    patient_dob: str,
    member_id: str,
    diagnosis_codes: list[str],
    procedure_code: str,
    clinical_summary: str,
    policy: dict[str, Any],
) -> dict[str, str]:
    """Build PDF field mappings from form data."""
    mappings = policy.get("form_field_mappings", {})

    result = {}

    if "patient_name" in mappings:
        result[mappings["patient_name"]] = patient_name
    if "patient_dob" in mappings:
        result[mappings["patient_dob"]] = patient_dob
    if "member_id" in mappings:
        result[mappings["member_id"]] = member_id
    if "diagnosis_primary" in mappings and diagnosis_codes:
        result[mappings["diagnosis_primary"]] = diagnosis_codes[0]
    if "diagnosis_secondary" in mappings and len(diagnosis_codes) > 1:
        result[mappings["diagnosis_secondary"]] = ", ".join(diagnosis_codes[1:])
    if "procedure_code" in mappings:
        result[mappings["procedure_code"]] = procedure_code
    if "clinical_summary" in mappings:
        result[mappings["clinical_summary"]] = clinical_summary
    if "date_of_service" in mappings:
        result[mappings["date_of_service"]] = date.today().isoformat()

    return result
