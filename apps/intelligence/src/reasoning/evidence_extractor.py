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
from src.models.policy import PolicyCriterion, PolicyDefinition

logger = logging.getLogger(__name__)


async def evaluate_criterion(
    criterion: PolicyCriterion | dict[str, Any],
    clinical_summary: str,
) -> EvidenceItem:
    """
    Evaluate a single policy criterion against clinical data using LLM.

    Args:
        criterion: PolicyCriterion or dict with 'id' and 'description'
        clinical_summary: Pre-built clinical data summary string

    Returns:
        EvidenceItem with evaluation result
    """
    if isinstance(criterion, PolicyCriterion):
        criterion_id = criterion.id
        criterion_desc = criterion.description
        lcd_section = criterion.lcd_section
    else:
        criterion_id = criterion.get("id", "unknown")
        criterion_desc = criterion.get("description", "")
        lcd_section = None

    system_prompt = (
        "You are a medical prior authorization analyst. Evaluate whether "
        "clinical evidence meets the specified criterion."
    )
    policy_ref = f"\nPolicy Reference: {lcd_section}" if lcd_section else ""
    user_prompt = f"""
Criterion: {criterion_desc}{policy_ref}

Clinical Data:
{clinical_summary}

Evaluate if this criterion is MET, NOT_MET, or UNCLEAR.
Indicate your confidence level: HIGH CONFIDENCE, MEDIUM CONFIDENCE, or LOW CONFIDENCE.
Provide a brief explanation of the evidence found.
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
        elif re.search(r"\bMET\b", response_upper):
            status = "MET"
        elif re.search(r"\bUNCLEAR\b", response_upper):
            status = "UNCLEAR"

        # Parse confidence signal from LLM response
        if "HIGH CONFIDENCE" in response_upper:
            confidence = 0.9
        elif "LOW CONFIDENCE" in response_upper:
            confidence = 0.5
        else:
            confidence = 0.7  # default MEDIUM

    return EvidenceItem(
        criterion_id=criterion_id,
        criterion_label=criterion_desc,
        status=status,
        evidence=evidence_text,
        source="LLM analysis",
        confidence=confidence,
    )


def _build_clinical_summary(
    clinical_bundle: ClinicalBundle,
    policy: PolicyDefinition | dict[str, Any] | None = None,
) -> str:
    """Build a clinical data summary string for LLM prompts."""
    patient_info = ""

    # Procedure being requested — critical context for LLM evaluation
    procedure_context = ""
    if isinstance(policy, PolicyDefinition):
        codes = ", ".join(policy.procedure_codes)
        procedure_context = f"Procedure Requested: {policy.policy_name} (CPT {codes})"
        if policy.lcd_reference:
            procedure_context += f" — LCD {policy.lcd_reference}"
    elif isinstance(policy, dict) and policy.get("procedure_codes"):
        codes = ", ".join(policy["procedure_codes"])
        procedure_context = f"Procedure Requested: CPT {codes}"

    conditions_text = ", ".join(
        [f"{c.display} ({c.code})" for c in clinical_bundle.conditions if c.display]
    ) or "None documented"

    observations_text = ", ".join(
        [
            f"{o.display or o.code}: {o.value} {o.unit or ''}"
            for o in clinical_bundle.observations
            if o.value
        ]
    ) or "None documented"

    procedures_text = ", ".join(
        [
            f"{p.display or p.code} ({p.status or 'unknown'})"
            for p in clinical_bundle.procedures
        ]
    ) or "None documented"

    if clinical_bundle.document_texts:
        document_text = "\n\n".join(clinical_bundle.document_texts)
    else:
        document_text = "No documents"

    return f"""
{patient_info}
{procedure_context}
Diagnoses: {conditions_text}
Prior Procedures: {procedures_text}
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
    criterion: PolicyCriterion | dict[str, Any],
    clinical_summary: str,
    semaphore: asyncio.Semaphore,
) -> EvidenceItem:
    """Evaluate a criterion with semaphore-bounded concurrency."""
    async with semaphore:
        return await evaluate_criterion(criterion, clinical_summary)


async def extract_evidence(
    clinical_bundle: ClinicalBundle,
    policy: PolicyDefinition | dict[str, Any],
) -> list[EvidenceItem]:
    """
    Extract evidence from clinical bundle using LLM to evaluate policy criteria.

    Evaluates criteria concurrently with a configurable concurrency limit.

    Args:
        clinical_bundle: FHIR clinical data bundle
        policy: PolicyDefinition or dict with criteria

    Returns:
        List of evidence items, one per policy criterion
    """
    if isinstance(policy, PolicyDefinition):
        criteria: list[PolicyCriterion | dict[str, Any]] = list(policy.criteria)
    else:
        criteria = policy.get("criteria", [])
    if not criteria:
        return []

    clinical_summary = _build_clinical_summary(clinical_bundle, policy)
    semaphore = _get_llm_semaphore()

    results = await asyncio.gather(
        *[_bounded_evaluate(c, clinical_summary, semaphore) for c in criteria],
        return_exceptions=True,
    )

    evidence_items: list[EvidenceItem] = []
    for i, result in enumerate(results):
        if isinstance(result, BaseException):
            crit = criteria[i]
            criterion_id = (
                crit.id if isinstance(crit, PolicyCriterion) else crit.get("id", "unknown")
            )
            criterion_label = (
                crit.description if isinstance(crit, PolicyCriterion) else crit.get("description", "")
            )
            logger.error("Criterion %s evaluation failed: %s", criterion_id, result)
            evidence_items.append(
                EvidenceItem(
                    criterion_id=criterion_id,
                    criterion_label=criterion_label,
                    status="UNCLEAR",
                    evidence=f"Evaluation error: {result}",
                    source="LLM analysis",
                    confidence=0.0,
                )
            )
        else:
            evidence_items.append(result)

    return evidence_items
