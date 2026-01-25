"""Evidence extraction from clinical data using LLM."""

import re
from typing import Any

from src.config import settings
from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem


EVIDENCE_EXTRACTION_PROMPT = """You are a clinical documentation specialist reviewing medical records for prior authorization.

PATIENT CONTEXT (Structured FHIR Data):
{structured_data}

POLICY REQUIREMENTS for {procedure_name}:
{policy_criteria}

TASK:
Extract evidence from the clinical data that supports or refutes each policy criterion.
For each criterion, determine:
1. Whether it is MET, NOT_MET, or UNCLEAR
2. The specific evidence found (quote the source if available)
3. Your confidence in this assessment (0.0 to 1.0)

Respond in JSON format with an array of evidence items."""


async def extract_evidence(
    clinical_bundle: ClinicalBundle,
    policy: dict[str, Any],
) -> list[EvidenceItem]:
    """
    Extract evidence from clinical data for each policy criterion.

    Uses LLM for complex reasoning, with pattern matching as fallback.
    """
    evidence_items: list[EvidenceItem] = []

    # Build structured data summary
    structured_data = _build_structured_summary(clinical_bundle)

    # Check each criterion
    for criterion in policy.get("criteria", []):
        criterion_id = criterion["id"]
        description = criterion["description"]

        # First try pattern matching for quick evidence
        pattern_evidence = _check_patterns(
            clinical_bundle, criterion.get("evidence_patterns", [])
        )

        if pattern_evidence:
            evidence_items.append(
                EvidenceItem(
                    criterion_id=criterion_id,
                    status="MET",
                    evidence=pattern_evidence,
                    source="Pattern matching on clinical data",
                    confidence=0.85,
                )
            )
        elif criterion_id == "diagnosis_present":
            # Special handling for diagnosis check
            diagnosis_evidence = _check_diagnosis(clinical_bundle, policy)
            evidence_items.append(diagnosis_evidence)
        else:
            # If no pattern match, use LLM or mark as unclear
            if settings.openai_api_key:
                llm_evidence = await _extract_with_llm(
                    structured_data, criterion, policy
                )
                evidence_items.append(llm_evidence)
            else:
                evidence_items.append(
                    EvidenceItem(
                        criterion_id=criterion_id,
                        status="UNCLEAR",
                        evidence="Unable to determine - LLM not configured",
                        source="System",
                        confidence=0.0,
                    )
                )

    return evidence_items


def _build_structured_summary(bundle: ClinicalBundle) -> str:
    """Build a text summary of structured clinical data."""
    parts = []

    if bundle.patient:
        parts.append(f"Patient: {bundle.patient.name}")
        if bundle.patient.birth_date:
            parts.append(f"DOB: {bundle.patient.birth_date}")

    if bundle.conditions:
        conditions_str = ", ".join(
            f"{c.code} ({c.display or 'Unknown'})" for c in bundle.conditions
        )
        parts.append(f"Active Conditions: {conditions_str}")

    if bundle.procedures:
        procedures_str = ", ".join(
            f"{p.code} ({p.display or 'Unknown'})" for p in bundle.procedures
        )
        parts.append(f"Recent Procedures: {procedures_str}")

    if bundle.observations:
        parts.append(f"Observations: {len(bundle.observations)} results")

    return "\n".join(parts)


def _check_patterns(bundle: ClinicalBundle, patterns: list[str]) -> str | None:
    """Check for evidence using regex patterns."""
    # Build searchable text from clinical data
    search_text = _build_structured_summary(bundle).lower()

    # Add any document text
    for doc_text in bundle.document_texts:
        search_text += "\n" + doc_text.lower()

    for pattern in patterns:
        match = re.search(pattern, search_text, re.IGNORECASE)
        if match:
            # Return the matched text with context
            start = max(0, match.start() - 50)
            end = min(len(search_text), match.end() + 50)
            return f"...{search_text[start:end]}..."

    return None


def _check_diagnosis(bundle: ClinicalBundle, policy: dict[str, Any]) -> EvidenceItem:
    """Check if patient has a qualifying diagnosis code."""
    primary_codes = set(policy.get("diagnosis_codes", {}).get("primary", []))
    supporting_codes = set(policy.get("diagnosis_codes", {}).get("supporting", []))
    all_valid_codes = primary_codes | supporting_codes

    patient_codes = {c.code for c in bundle.conditions}
    matching_codes = patient_codes & all_valid_codes

    if matching_codes:
        matching_primary = matching_codes & primary_codes
        if matching_primary:
            return EvidenceItem(
                criterion_id="diagnosis_present",
                status="MET",
                evidence=f"Primary diagnosis codes found: {', '.join(matching_primary)}",
                source="FHIR Condition resources",
                confidence=0.95,
            )
        else:
            return EvidenceItem(
                criterion_id="diagnosis_present",
                status="MET",
                evidence=f"Supporting diagnosis codes found: {', '.join(matching_codes)}",
                source="FHIR Condition resources",
                confidence=0.85,
            )
    else:
        return EvidenceItem(
            criterion_id="diagnosis_present",
            status="NOT_MET",
            evidence=f"No qualifying diagnosis codes found. Patient codes: {', '.join(patient_codes) or 'None'}",
            source="FHIR Condition resources",
            confidence=0.90,
        )


async def _extract_with_llm(
    structured_data: str,
    criterion: dict[str, Any],
    policy: dict[str, Any],
) -> EvidenceItem:
    """Use LLM to extract evidence for a criterion."""
    try:
        from openai import AsyncOpenAI

        client = AsyncOpenAI(api_key=settings.openai_api_key)

        prompt = f"""Analyze this clinical data for evidence of: {criterion['description']}

Clinical Data:
{structured_data}

Respond with:
1. STATUS: MET, NOT_MET, or UNCLEAR
2. EVIDENCE: The specific text or finding that supports your conclusion
3. CONFIDENCE: A number from 0.0 to 1.0

Format your response as:
STATUS: <status>
EVIDENCE: <evidence>
CONFIDENCE: <number>"""

        response = await client.chat.completions.create(
            model=settings.openai_model,
            messages=[
                {"role": "system", "content": "You are a clinical documentation specialist."},
                {"role": "user", "content": prompt},
            ],
            temperature=0,
            max_tokens=500,
        )

        content = response.choices[0].message.content or ""

        # Parse response
        status = "UNCLEAR"
        evidence = "Unable to determine"
        confidence = 0.5

        for line in content.split("\n"):
            if line.startswith("STATUS:"):
                status_val = line.replace("STATUS:", "").strip().upper()
                if status_val in ("MET", "NOT_MET", "UNCLEAR"):
                    status = status_val
            elif line.startswith("EVIDENCE:"):
                evidence = line.replace("EVIDENCE:", "").strip()
            elif line.startswith("CONFIDENCE:"):
                try:
                    confidence = float(line.replace("CONFIDENCE:", "").strip())
                except ValueError:
                    pass

        return EvidenceItem(
            criterion_id=criterion["id"],
            status=status,  # type: ignore
            evidence=evidence,
            source="LLM analysis",
            confidence=confidence,
        )

    except Exception as e:
        return EvidenceItem(
            criterion_id=criterion["id"],
            status="UNCLEAR",
            evidence=f"LLM analysis failed: {str(e)}",
            source="System",
            confidence=0.0,
        )
