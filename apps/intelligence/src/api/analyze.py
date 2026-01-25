"""Analysis endpoint for processing clinical data and generating PA form."""

from typing import Any

from fastapi import APIRouter, HTTPException, UploadFile, File
from pydantic import BaseModel

from src.models.clinical_bundle import ClinicalBundle
from src.models.pa_form import PAFormResponse
from src.policies.mri_lumbar import MRI_LUMBAR_POLICY
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

    This endpoint:
    1. Validates the procedure code against supported policies
    2. Extracts evidence from clinical data
    3. Evaluates against policy criteria
    4. Generates form field values
    """
    # Check if procedure is supported
    if request.procedure_code not in MRI_LUMBAR_POLICY["procedure_codes"]:
        raise HTTPException(
            status_code=400,
            detail=f"Procedure code {request.procedure_code} not supported",
        )

    # Parse clinical data into structured format
    clinical_bundle = ClinicalBundle.from_dict(request.patient_id, request.clinical_data)

    # Extract evidence from clinical data
    evidence = await extract_evidence(clinical_bundle, MRI_LUMBAR_POLICY)

    # Generate form data based on evidence
    form_response = await generate_form_data(clinical_bundle, evidence, MRI_LUMBAR_POLICY)

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

    Processes multipart form data including:
    - **patient_id**: Unique patient identifier
    - **procedure_code**: CPT/HCPCS code for the procedure
    - **clinical_data**: JSON string of clinical data
    - **documents**: PDF files containing clinical documentation

    Returns the same PA form response as the standard analyze endpoint.
    """
    import json

    # Parse clinical data
    try:
        clinical_data_dict = json.loads(clinical_data)
    except json.JSONDecodeError as e:
        raise HTTPException(status_code=400, detail=f"Invalid clinical data JSON: {e}")

    # Process documents if provided
    document_texts: list[str] = []
    for doc in documents:
        if doc.content_type == "application/pdf":
            # In production, use LlamaParse here
            content = await doc.read()
            document_texts.append(f"[Document: {doc.filename}, {len(content)} bytes]")

    # Build request and process
    request = AnalyzeRequest(
        patient_id=patient_id,
        procedure_code=procedure_code,
        clinical_data=clinical_data_dict,
    )

    return await analyze(request)
