"""Analysis endpoint for processing clinical data and generating PA form.

Uses LLM-powered reasoning to extract evidence and generate PA form responses.
"""

from typing import Any

from fastapi import APIRouter, File, HTTPException, UploadFile
from pydantic import BaseModel

from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import PAFormResponse
from src.parsers.pdf_parser import parse_pdf
from src.policies.example_policy import EXAMPLE_POLICY
from src.reasoning.evidence_extractor import extract_evidence
from src.reasoning.form_generator import generate_form_data

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

    Uses LLM to extract evidence from clinical data and generate PA form.
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

    # Load policy with requested procedure code
    policy = {**EXAMPLE_POLICY, "procedure_codes": [request.procedure_code]}

    # Extract evidence using LLM
    evidence = await extract_evidence(bundle, policy)

    # Generate form data using LLM
    form_response = await generate_form_data(bundle, evidence, policy)

    return form_response


@router.post("/with-documents", response_model=PAFormResponse)
async def analyze_with_documents(
    patient_id: str,
    procedure_code: str,
    clinical_data: str,  # JSON string
    documents: list[UploadFile] = File(default=[]),
) -> PAFormResponse:
    """
    Analyze clinical data with attached PDF documents.

    Parses PDF documents and includes extracted text in the analysis.
    """
    import json

    # Parse clinical data
    try:
        clinical_data_dict = json.loads(clinical_data)
    except json.JSONDecodeError as e:
        raise HTTPException(status_code=400, detail=f"Invalid clinical data JSON: {e}")

    # Check if procedure is supported
    if procedure_code not in SUPPORTED_PROCEDURE_CODES:
        raise HTTPException(
            status_code=400,
            detail=f"Procedure code {procedure_code} not supported",
        )

    # Parse clinical data into structured format
    bundle = ClinicalBundle.from_dict(patient_id, clinical_data_dict)

    # Parse PDF documents and add to bundle
    document_texts = []
    for document in documents:
        pdf_bytes = await document.read()
        try:
            text = await parse_pdf(pdf_bytes)
            document_texts.append(text)
        except Exception as e:
            # Log error but continue processing
            document_texts.append(f"[PDF parsing error: {e}]")

    bundle.document_texts = document_texts

    # Validate required patient data
    patient = bundle.patient
    if not patient or not patient.birth_date:
        raise HTTPException(
            status_code=400,
            detail="patient.birth_date is required",
        )

    # Load policy with requested procedure code
    policy = {**EXAMPLE_POLICY, "procedure_codes": [procedure_code]}

    # Extract evidence using LLM
    evidence = await extract_evidence(bundle, policy)

    # Generate form data using LLM
    form_response = await generate_form_data(bundle, evidence, policy)

    return form_response


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
