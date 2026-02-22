"""Analysis endpoint for processing clinical data and generating PA form.

Uses LLM-powered reasoning to extract evidence and generate PA form responses.
"""

import asyncio
from typing import Any

from fastapi import APIRouter, File, HTTPException, UploadFile
from pydantic import BaseModel

from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import PAFormResponse
from src.parsers.pdf_parser import parse_pdf
from src.policies.registry import registry
from src.reasoning.evidence_extractor import extract_evidence
from src.reasoning.form_generator import generate_form_data

router = APIRouter()


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
    Resolves policy from registry; unknown CPT codes fall back to generic policy.
    """
    # Parse clinical data into structured format
    bundle = ClinicalBundle.from_dict(request.patient_id, request.clinical_data)

    # Validate required patient data
    patient = bundle.patient
    if not patient or not patient.birth_date:
        raise HTTPException(
            status_code=400,
            detail="patient.birth_date is required",
        )

    # Resolve policy from registry (no more 400 rejection for unsupported CPTs)
    policy = registry.resolve(request.procedure_code)

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

    bundle = ClinicalBundle.from_dict(patient_id, clinical_data_dict)

    # Read all document bytes, then parse PDFs in parallel
    pdf_bytes_list = [await doc.read() for doc in documents]
    document_texts = list(await asyncio.gather(*[parse_pdf(b) for b in pdf_bytes_list]))
    bundle.document_texts = document_texts

    # Validate required patient data
    patient = bundle.patient
    if not patient or not patient.birth_date:
        raise HTTPException(
            status_code=400,
            detail="patient.birth_date is required",
        )

    # Resolve policy from registry
    policy = registry.resolve(procedure_code)

    # Extract evidence using LLM
    evidence = await extract_evidence(bundle, policy)

    # Generate form data using LLM
    form_response = await generate_form_data(bundle, evidence, policy)

    return form_response
