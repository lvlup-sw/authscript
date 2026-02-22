"""Prior Authorization form response models."""

from typing import Literal

from pydantic import BaseModel, Field


class EvidenceItem(BaseModel):
    """Evidence item supporting a policy criterion."""

    criterion_id: str = Field(description="ID of the policy criterion")
    status: Literal["MET", "NOT_MET", "UNCLEAR"] = Field(description="Criterion status")
    evidence: str = Field(description="Extracted evidence text")
    source: str = Field(description="Source of the evidence")
    confidence: float = Field(ge=0.0, le=1.0, description="Confidence score")


class PAFormResponse(BaseModel):
    """Complete PA form response from analysis."""

    patient_name: str = Field(description="Patient full name")
    patient_dob: str = Field(description="Patient date of birth (YYYY-MM-DD)")
    member_id: str = Field(description="Insurance member ID")
    diagnosis_codes: list[str] = Field(description="ICD-10 diagnosis codes")
    procedure_code: str = Field(description="CPT procedure code")
    clinical_summary: str = Field(description="AI-generated medical necessity summary")
    supporting_evidence: list[EvidenceItem] = Field(description="Evidence items")
    recommendation: Literal["APPROVE", "NEED_INFO", "MANUAL_REVIEW"] = Field(
        description="AI recommendation"
    )
    confidence_score: float = Field(ge=0.0, le=1.0, description="Overall confidence")
    field_mappings: dict[str, str] = Field(
        description="PDF field name to value mappings"
    )
    policy_id: str | None = Field(default=None, description="Policy ID used for evaluation")
    lcd_reference: str | None = Field(default=None, description="LCD reference number if applicable")
