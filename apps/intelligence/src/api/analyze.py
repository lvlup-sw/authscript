"""Analysis endpoint for processing clinical data and generating PA form.

This is a stub implementation that returns APPROVE for all requests.
Production implementation would include policy evaluation and LLM reasoning.
"""

from typing import Any

from fastapi import APIRouter, File, HTTPException, UploadFile
from pydantic import BaseModel

from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import PAFormResponse

router = APIRouter()

# Supported procedure codes (MRI Lumbar Spine)
SUPPORTED_PROCEDURE_CODES = {"72148", "72149", "72158"}


class AnalyzeRequest(BaseModel):
    """Request payload for analysis endpoint."""

    patient_id: str
    procedure_code: str
    clinical_data: dict[str, Any]


@router.post("", response_model=PAFormResponse)
async def analyze(request: AnalyzeRequest) -> PAFormResponse:
    """
    Analyze clinical data and generate PA form response.

    STUB IMPLEMENTATION: Always returns APPROVE with 1.0 confidence.
    Production version would evaluate clinical data against payer policies.
    """
    # Check if procedure is supported
    if request.procedure_code not in SUPPORTED_PROCEDURE_CODES:
        raise HTTPException(
            status_code=400,
            detail=f"Procedure code {request.procedure_code} not supported",
        )

    # Parse clinical data into structured format
    bundle = ClinicalBundle.from_dict(request.patient_id, request.clinical_data)

    # Validate required patient data
    patient = bundle.patient
    if not patient or not patient.birth_date:
        raise HTTPException(
            status_code=400,
            detail="patient.birth_date is required",
        )

    # Build stub response
    return PAFormResponse(
        patient_name=patient.name,
        patient_dob=patient.birth_date.isoformat(),
        member_id=patient.member_id if patient.member_id else "Unknown",
        diagnosis_codes=[c.code for c in bundle.conditions] if bundle.conditions else [],
        procedure_code=request.procedure_code,
        clinical_summary="Awaiting production configuration",
        supporting_evidence=[],
        recommendation="APPROVE",
        confidence_score=1.0,
        field_mappings=_build_field_mappings(bundle, request.procedure_code),
    )


@router.post("/with-documents", response_model=PAFormResponse)
async def analyze_with_documents(
    patient_id: str,
    procedure_code: str,
    clinical_data: str,  # JSON string
    documents: list[UploadFile] = File(default=[]),
) -> PAFormResponse:
    """
    Analyze clinical data with attached PDF documents.

    STUB IMPLEMENTATION: Documents are acknowledged but not processed.
    Production version would extract text and analyze documents.
    """
    import json

    # Parse clinical data
    try:
        clinical_data_dict = json.loads(clinical_data)
    except json.JSONDecodeError as e:
        raise HTTPException(status_code=400, detail=f"Invalid clinical data JSON: {e}")

    # Build request and process (documents ignored in stub)
    request = AnalyzeRequest(
        patient_id=patient_id,
        procedure_code=procedure_code,
        clinical_data=clinical_data_dict,
    )

    return await analyze(request)


def _build_field_mappings(bundle: ClinicalBundle, procedure_code: str) -> dict[str, str]:
    """Build PDF field mappings from clinical bundle."""
    patient_name = bundle.patient.name if bundle.patient else "Unknown"
    patient_dob = (
        bundle.patient.birth_date.isoformat()
        if bundle.patient and bundle.patient.birth_date
        else "Unknown"
    )
    member_id = (
        bundle.patient.member_id
        if bundle.patient and bundle.patient.member_id
        else "Unknown"
    )
    diagnosis_codes = ", ".join(c.code for c in bundle.conditions) if bundle.conditions else ""

    return {
        "PatientName": patient_name,
        "PatientDOB": patient_dob,
        "MemberID": member_id,
        "DiagnosisCodes": diagnosis_codes,
        "ProcedureCode": procedure_code,
        "ClinicalSummary": "Awaiting production configuration",
        "ProviderSignature": "",
        "Date": "",
    }
