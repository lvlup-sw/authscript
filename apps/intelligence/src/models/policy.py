"""Policy data models for LCD-based prior authorization criteria."""

from pydantic import BaseModel, Field


class PolicyCriterion(BaseModel):
    """A single criterion within a policy definition."""

    id: str = Field(description="Unique criterion identifier")
    description: str = Field(description="Human-readable criterion description")
    weight: float = Field(ge=0.0, le=1.0, description="Relative weight of this criterion")
    required: bool = Field(default=False, description="Whether this criterion is mandatory")
    lcd_section: str | None = Field(
        default=None, description="LCD article section reference"
    )
    bypasses: list[str] = Field(
        default_factory=list,
        description="List of criterion IDs that bypass this criterion when met",
    )


class PolicyDefinition(BaseModel):
    """Complete policy definition for a prior authorization procedure."""

    policy_id: str = Field(description="Unique policy identifier")
    policy_name: str = Field(description="Human-readable policy name")
    lcd_reference: str | None = Field(
        default=None, description="LCD article reference number"
    )
    lcd_title: str | None = Field(default=None, description="LCD article title")
    lcd_contractor: str | None = Field(
        default=None, description="LCD Medicare contractor"
    )
    payer: str = Field(description="Payer name")
    procedure_codes: list[str] = Field(description="CPT procedure codes covered")
    diagnosis_codes: list[str] = Field(
        default_factory=list, description="ICD-10 diagnosis codes"
    )
    criteria: list[PolicyCriterion] = Field(description="Policy evaluation criteria")
