"""Policy data models for prior authorization criteria."""

from dataclasses import dataclass, field


@dataclass
class PolicyCriterion:
    """A single criterion within a policy definition."""

    id: str
    description: str
    weight: float = 1.0
    required: bool = False


@dataclass
class PolicyDefinition:
    """Complete policy definition for a procedure or set of procedures."""

    policy_id: str
    policy_name: str
    payer: str
    procedure_codes: list[str] = field(default_factory=list)
    criteria: list[PolicyCriterion] = field(default_factory=list)
    lcd_reference: str | None = None
