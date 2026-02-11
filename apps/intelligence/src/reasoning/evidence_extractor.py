"""Evidence extraction from clinical data using LLM.

Uses LLM to evaluate policy criteria against clinical data.
"""

import asyncio
import logging
import re
from typing import Any, Literal

from src.config import settings
from src.llm_client import chat_completion
from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem

logger = logging.getLogger(__name__)


async def evaluate_criterion(
    criterion: dict[str, Any],
    clinical_summary: str,
) -> EvidenceItem:
    """
    Evaluate a single policy criterion against clinical data using LLM.

    Args:
        criterion: Policy criterion dict with 'id' and 'description'
        clinical_summary: Pre-built clinical data summary string

    Returns:
        EvidenceItem with evaluation result
    """
    criterion_id = criterion.get("id", "unknown")
    criterion_desc = criterion.get("description", "")

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
        # Use regex to handle "NOT MET", "NOT_MET", "NOTMET" variants
        if re.search(r"\bNOT[\s_]?MET\b", response_upper):
            status = "NOT_MET"
            confidence = 0.8
        elif re.search(r"\bMET\b", response_upper):
            status = "MET"
            confidence = 0.8
        elif re.search(r"\bUNCLEAR\b", response_upper):
            status = "UNCLEAR"
            confidence = 0.5

    return EvidenceItem(
        criterion_id=criterion_id,
        status=status,
        evidence=evidence_text,
        source="LLM analysis",
        confidence=confidence,
    )


def _build_clinical_summary(clinical_bundle: ClinicalBundle) -> str:
    """Build a clinical data summary string for LLM prompts."""
    patient_info = ""
    if clinical_bundle.patient:
        patient_info = "Patient: [REDACTED]"

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

    return f"""
{patient_info}
Diagnoses: {conditions_text}
Observations: {observations_text}
Documents:
{document_text}
"""


_llm_semaphore: asyncio.Semaphore | None = None


def _get_llm_semaphore() -> asyncio.Semaphore:
    """Get a semaphore for bounding concurrent LLM calls (lazy singleton)."""
    global _llm_semaphore
    if _llm_semaphore is None:
        _llm_semaphore = asyncio.Semaphore(settings.llm_max_concurrent)
    return _llm_semaphore


async def _bounded_evaluate(
    criterion: dict[str, Any],
    clinical_summary: str,
    semaphore: asyncio.Semaphore,
) -> EvidenceItem:
    """Evaluate a criterion with semaphore-bounded concurrency."""
    async with semaphore:
        return await evaluate_criterion(criterion, clinical_summary)


async def extract_evidence(
    clinical_bundle: ClinicalBundle,
    policy: dict[str, Any],
) -> list[EvidenceItem]:
    """
    Extract evidence from clinical bundle using LLM to evaluate policy criteria.

    Evaluates criteria concurrently with a configurable concurrency limit.

    Args:
        clinical_bundle: FHIR clinical data bundle
        policy: Policy definition with criteria

    Returns:
        List of evidence items, one per policy criterion
    """
    criteria = policy.get("criteria", [])
    if not criteria:
        return []

    clinical_summary = _build_clinical_summary(clinical_bundle)
    semaphore = _get_llm_semaphore()

    results = await asyncio.gather(
        *[_bounded_evaluate(c, clinical_summary, semaphore) for c in criteria],
        return_exceptions=True,
    )

    evidence_items: list[EvidenceItem] = []
    for i, result in enumerate(results):
        if isinstance(result, BaseException):
            criterion_id = criteria[i].get("id", "unknown")
            logger.error("Criterion %s evaluation failed: %s", criterion_id, result)
            evidence_items.append(
                EvidenceItem(
                    criterion_id=criterion_id,
                    status="UNCLEAR",
                    evidence=f"Evaluation error: {result}",
                    source="LLM analysis",
                    confidence=0.0,
                )
            )
        else:
            evidence_items.append(result)

    return evidence_items
