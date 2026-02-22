"""Policy data models for LCD-backed prior authorization criteria."""

from pydantic import BaseModel


class PolicyCriterion(BaseModel):
    """A single criterion from a coverage policy."""

    id: str
    description: str
    weight: float  # 0.0-1.0, clinical importance
    required: bool = False  # Hard gate — if NOT_MET, caps score
    lcd_section: str | None = None  # e.g. "L34220 §4.2"
    bypasses: list[str] = []  # criterion IDs this one bypasses when MET


class PolicyDefinition(BaseModel):
    """Complete policy definition with LCD metadata."""

    policy_id: str
    policy_name: str
    lcd_reference: str | None = None  # e.g. "L34220"
    lcd_title: str | None = None
    lcd_contractor: str | None = None
    payer: str
    procedure_codes: list[str]
    diagnosis_codes: list[str] = []
    criteria: list[PolicyCriterion]
