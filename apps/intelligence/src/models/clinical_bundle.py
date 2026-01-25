"""Clinical data models for incoming FHIR data."""

from dataclasses import dataclass, field
from datetime import date
from typing import Any


@dataclass
class PatientInfo:
    """Patient demographic information."""

    name: str
    birth_date: date | None = None
    gender: str | None = None
    member_id: str | None = None


@dataclass
class Condition:
    """Clinical condition (diagnosis)."""

    code: str
    system: str | None = None
    display: str | None = None
    clinical_status: str | None = None


@dataclass
class Observation:
    """Clinical observation (lab result, vital sign)."""

    code: str
    system: str | None = None
    display: str | None = None
    value: str | None = None
    unit: str | None = None


@dataclass
class Procedure:
    """Clinical procedure."""

    code: str
    system: str | None = None
    display: str | None = None
    status: str | None = None


@dataclass
class ClinicalBundle:
    """Aggregated clinical data from Gateway service."""

    patient_id: str
    patient: PatientInfo | None = None
    conditions: list[Condition] = field(default_factory=list)
    observations: list[Observation] = field(default_factory=list)
    procedures: list[Procedure] = field(default_factory=list)
    document_texts: list[str] = field(default_factory=list)

    @classmethod
    def from_dict(cls, patient_id: str, data: dict[str, Any]) -> "ClinicalBundle":
        """Create ClinicalBundle from dictionary (API request)."""
        patient_data = data.get("patient")
        patient = None
        if patient_data:
            birth_date = None
            if patient_data.get("birth_date"):
                try:
                    birth_date = date.fromisoformat(patient_data["birth_date"])
                except ValueError:
                    pass

            patient = PatientInfo(
                name=patient_data.get("name", "Unknown"),
                birth_date=birth_date,
                gender=patient_data.get("gender"),
                member_id=patient_data.get("member_id"),
            )

        conditions = [
            Condition(
                code=c.get("code", ""),
                system=c.get("system"),
                display=c.get("display"),
                clinical_status=c.get("clinical_status"),
            )
            for c in data.get("conditions", [])
        ]

        observations = [
            Observation(
                code=o.get("code", ""),
                system=o.get("system"),
                display=o.get("display"),
                value=o.get("value"),
                unit=o.get("unit"),
            )
            for o in data.get("observations", [])
        ]

        procedures = [
            Procedure(
                code=p.get("code", ""),
                system=p.get("system"),
                display=p.get("display"),
                status=p.get("status"),
            )
            for p in data.get("procedures", [])
        ]

        return cls(
            patient_id=patient_id,
            patient=patient,
            conditions=conditions,
            observations=observations,
            procedures=procedures,
        )
