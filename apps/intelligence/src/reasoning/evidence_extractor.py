"""Evidence extraction from clinical data using LLM.

Uses LLM to evaluate policy criteria against clinical data.
"""

from typing import Any, Literal

from src.llm_client import chat_completion
from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem


async def extract_evidence(
    clinical_bundle: ClinicalBundle,
    policy: dict[str, Any],
) -> list[EvidenceItem]:
    """
    Extract evidence from clinical bundle using LLM to evaluate policy criteria.

    Args:
        clinical_bundle: FHIR clinical data bundle
        policy: Policy definition with criteria

    Returns:
        List of evidence items, one per policy criterion
    """
    evidence_items = []

    # Build clinical data summary for LLM
    patient_info = ""
    if clinical_bundle.patient:
        patient_info = f"Patient: {clinical_bundle.patient.name}"
        if clinical_bundle.patient.birth_date:
            patient_info += f", DOB: {clinical_bundle.patient.birth_date.isoformat()}"

    conditions_text = ", ".join(
        [f"{c.code} ({c.display})" for c in clinical_bundle.conditions if c.display]
    ) or "None documented"

    observations_text = ", ".join(
        [
            f"{o.display or o.code}: {o.value} {o.unit or ''}"
            for o in clinical_bundle.observations
            if o.value
        ]
    ) or "None documented"

    if clinical_bundle.document_texts:
        document_text = "\n\n".join(clinical_bundle.document_texts)
    else:
        document_text = "No documents"

    clinical_summary = f"""
{patient_info}
Diagnoses: {conditions_text}
Observations: {observations_text}
Documents:
{document_text}
"""

    # Evaluate each criterion using LLM
    for criterion in policy.get("criteria", []):
        criterion_id = criterion.get("id", "unknown")
        criterion_desc = criterion.get("description", "")

        # Simple LLM prompt to evaluate criterion
        system_prompt = (
            "You are a medical prior authorization analyst. Evaluate whether "
            "clinical evidence meets the specified criterion."
        )
        user_prompt = f"""
Criterion: {criterion_desc}

Clinical Data:
{clinical_summary}

Evaluate if this criterion is MET, NOT_MET, or UNCLEAR. Provide a brief
explanation of the evidence found.
"""

        llm_response = await chat_completion(
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            temperature=0.3,
            max_tokens=1000,
        )

        # Parse LLM response to determine status
        status: Literal["MET", "NOT_MET", "UNCLEAR"] = "UNCLEAR"
        evidence_text = llm_response or "No response from LLM"
        confidence = 0.5

        if llm_response:
            response_upper = llm_response.upper()
            # Check NOT_MET first since it contains "MET" as substring
            if "NOT_MET" in response_upper:
                status = "NOT_MET"
                confidence = 0.8
            elif "MET" in response_upper:
                status = "MET"
                confidence = 0.8
            elif "UNCLEAR" in response_upper:
                status = "UNCLEAR"
                confidence = 0.5

        evidence_items.append(
            EvidenceItem(
                criterion_id=criterion_id,
                status=status,
                evidence=evidence_text,
                source="LLM analysis",
                confidence=confidence,
            )
        )

    return evidence_items
