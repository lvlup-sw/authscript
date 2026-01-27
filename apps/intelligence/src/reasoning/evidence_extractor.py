"""STUB: Evidence extraction from clinical data.

Production implementation will use LLM and pattern matching to extract
evidence from clinical bundles and evaluate policy criteria.
"""

from typing import Any

from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import EvidenceItem


async def extract_evidence(
    clinical_bundle: ClinicalBundle,
    policy: dict[str, Any],
) -> list[EvidenceItem]:
    """
    STUB: Return MET status for all policy criteria.

    Production implementation will:
    1. Build structured data summary from clinical bundle
    2. Check evidence patterns via regex matching
    3. Use LLM for complex criterion evaluation
    4. Return detailed evidence items with confidence scores

    Args:
        clinical_bundle: FHIR clinical data bundle
        policy: Policy definition with criteria

    Returns:
        List of evidence items, one per policy criterion
    """
    return [
        EvidenceItem(
            criterion_id=criterion["id"],
            status="MET",
            evidence="STUB: Evidence would be extracted from clinical data",
            source="Stub implementation",
            confidence=0.90,
        )
        for criterion in policy.get("criteria", [])
    ]
