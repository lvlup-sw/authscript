"""Tests for form generator."""

from datetime import date
from unittest.mock import AsyncMock, patch

import pytest

from src.models.clinical_bundle import ClinicalBundle, Condition, PatientInfo
from src.models.pa_form import EvidenceItem
from src.reasoning.form_generator import (
    _build_field_mappings,
    _calculate_recommendation,
    generate_form_data,
)


@pytest.fixture
def sample_policy() -> dict:
    """Create a sample policy for testing."""
    return {
        "criteria": [
            {"id": "conservative_therapy", "required": True},
            {"id": "neurological_symptoms", "required": True},
        ],
        "procedure_codes": ["72148"],
        "form_field_mappings": {
            "patient_name": "PatientFullName",
            "patient_dob": "DateOfBirth",
            "member_id": "MemberID",
            "diagnosis_primary": "PrimaryDiagnosis",
            "diagnosis_secondary": "SecondaryDiagnosis",
            "procedure_code": "ProcedureCode",
            "clinical_summary": "ClinicalNotes",
            "date_of_service": "ServiceDate",
        },
    }


@pytest.fixture
def sample_bundle() -> ClinicalBundle:
    """Create a sample clinical bundle for testing."""
    return ClinicalBundle(
        patient_id="test-001",
        patient=PatientInfo(
            name="John Doe",
            birth_date=date(1980, 5, 15),
            member_id="MEM123456",
        ),
        conditions=[
            Condition(
                code="M54.5",
                display="Low back pain",
                clinical_status="active",
            ),
        ],
    )


@pytest.fixture
def evidence_all_met() -> list[EvidenceItem]:
    """Evidence with all criteria met."""
    return [
        EvidenceItem(
            criterion_id="conservative_therapy",
            status="MET",
            evidence="Physical therapy completed for 6 weeks",
            source="clinical_notes",
            confidence=0.9,
        ),
        EvidenceItem(
            criterion_id="neurological_symptoms",
            status="MET",
            evidence="Radiculopathy with weakness",
            source="clinical_notes",
            confidence=0.85,
        ),
    ]


@pytest.fixture
def evidence_partial_met() -> list[EvidenceItem]:
    """Evidence with partial criteria met."""
    return [
        EvidenceItem(
            criterion_id="conservative_therapy",
            status="MET",
            evidence="Physical therapy completed",
            source="clinical_notes",
            confidence=0.8,
        ),
        EvidenceItem(
            criterion_id="neurological_symptoms",
            status="NOT_MET",
            evidence="No neurological symptoms documented",
            source="clinical_notes",
            confidence=0.7,
        ),
    ]


@pytest.fixture
def evidence_none_met() -> list[EvidenceItem]:
    """Evidence with no criteria met."""
    return [
        EvidenceItem(
            criterion_id="conservative_therapy",
            status="NOT_MET",
            evidence="No conservative therapy documented",
            source="clinical_notes",
            confidence=0.6,
        ),
        EvidenceItem(
            criterion_id="neurological_symptoms",
            status="NOT_MET",
            evidence="No neurological symptoms",
            source="clinical_notes",
            confidence=0.5,
        ),
    ]


class TestCalculateRecommendation:
    """Tests for _calculate_recommendation function."""

    def test_approve_when_all_required_criteria_met(
        self, sample_policy: dict, evidence_all_met: list[EvidenceItem]
    ) -> None:
        """Should return APPROVE when all required criteria are MET with high confidence."""
        recommendation, confidence = _calculate_recommendation(
            evidence_all_met, sample_policy
        )

        assert recommendation == "APPROVE"
        assert confidence >= 0.8

    def test_manual_review_when_partial_criteria_met(
        self, sample_policy: dict, evidence_partial_met: list[EvidenceItem]
    ) -> None:
        """Should return MANUAL_REVIEW when at least 50% criteria are met."""
        recommendation, confidence = _calculate_recommendation(
            evidence_partial_met, sample_policy
        )

        assert recommendation == "MANUAL_REVIEW"
        assert 0.0 <= confidence <= 1.0

    def test_need_info_when_insufficient_criteria(
        self, sample_policy: dict, evidence_none_met: list[EvidenceItem]
    ) -> None:
        """Should return NEED_INFO when less than 50% criteria are met."""
        recommendation, confidence = _calculate_recommendation(
            evidence_none_met, sample_policy
        )

        assert recommendation == "NEED_INFO"
        assert 0.0 <= confidence <= 1.0

    def test_neurological_red_flags_bypass_conservative_therapy(
        self, sample_policy: dict
    ) -> None:
        """Neurological symptoms should bypass conservative therapy requirement."""
        evidence = [
            EvidenceItem(
                criterion_id="neurological_symptoms",
                status="MET",
                evidence="Severe radiculopathy with motor weakness",
                source="clinical_notes",
                confidence=0.95,
            ),
            EvidenceItem(
                criterion_id="conservative_therapy",
                status="NOT_MET",
                evidence="No conservative therapy documented",
                source="clinical_notes",
                confidence=0.5,
            ),
        ]

        recommendation, confidence = _calculate_recommendation(evidence, sample_policy)

        # Should approve because neuro symptoms bypass conservative therapy requirement
        assert recommendation == "APPROVE"
        assert confidence >= 0.8

    def test_empty_evidence_list(self, sample_policy: dict) -> None:
        """Should return NEED_INFO with empty evidence."""
        recommendation, confidence = _calculate_recommendation([], sample_policy)

        assert recommendation == "NEED_INFO"
        assert 0.0 <= confidence <= 1.0

    def test_no_required_criteria_in_policy(self) -> None:
        """Should handle policy with no required criteria."""
        policy = {"criteria": [{"id": "optional_criterion", "required": False}]}
        evidence = [
            EvidenceItem(
                criterion_id="optional_criterion",
                status="MET",
                evidence="Optional criterion met",
                source="clinical_notes",
                confidence=0.9,
            )
        ]

        recommendation, confidence = _calculate_recommendation(evidence, policy)

        # With no required criteria, met_ratio is 0.0
        assert recommendation == "NEED_INFO"

    def test_empty_criteria_in_policy(self) -> None:
        """Should handle policy with empty criteria list."""
        policy: dict = {"criteria": []}

        recommendation, confidence = _calculate_recommendation([], policy)

        assert recommendation == "NEED_INFO"
        assert confidence == 0.3  # 0.5 * 0.6


class TestBuildFieldMappings:
    """Tests for _build_field_mappings function."""

    def test_maps_all_fields_correctly(self, sample_policy: dict) -> None:
        """Should map all available fields to PDF form fields."""
        result = _build_field_mappings(
            patient_name="John Doe",
            patient_dob="1980-05-15",
            member_id="MEM123456",
            diagnosis_codes=["M54.5", "M51.16"],
            procedure_code="72148",
            clinical_summary="Test clinical summary",
            policy=sample_policy,
        )

        assert result["PatientFullName"] == "John Doe"
        assert result["DateOfBirth"] == "1980-05-15"
        assert result["MemberID"] == "MEM123456"
        assert result["PrimaryDiagnosis"] == "M54.5"
        assert result["SecondaryDiagnosis"] == "M51.16"
        assert result["ProcedureCode"] == "72148"
        assert result["ClinicalNotes"] == "Test clinical summary"
        assert result["ServiceDate"] == date.today().isoformat()

    def test_single_diagnosis_code(self, sample_policy: dict) -> None:
        """Should handle single diagnosis code without secondary."""
        result = _build_field_mappings(
            patient_name="Jane Smith",
            patient_dob="1990-01-01",
            member_id="MEM789",
            diagnosis_codes=["M54.5"],
            procedure_code="72148",
            clinical_summary="Summary",
            policy=sample_policy,
        )

        assert result["PrimaryDiagnosis"] == "M54.5"
        assert "SecondaryDiagnosis" not in result

    def test_empty_diagnosis_codes(self, sample_policy: dict) -> None:
        """Should handle empty diagnosis codes list."""
        result = _build_field_mappings(
            patient_name="Jane Smith",
            patient_dob="1990-01-01",
            member_id="MEM789",
            diagnosis_codes=[],
            procedure_code="72148",
            clinical_summary="Summary",
            policy=sample_policy,
        )

        assert "PrimaryDiagnosis" not in result
        assert "SecondaryDiagnosis" not in result

    def test_no_field_mappings_in_policy(self) -> None:
        """Should return empty dict when no mappings configured."""
        policy: dict = {"criteria": [], "procedure_codes": ["72148"]}

        result = _build_field_mappings(
            patient_name="John Doe",
            patient_dob="1980-05-15",
            member_id="MEM123",
            diagnosis_codes=["M54.5"],
            procedure_code="72148",
            clinical_summary="Summary",
            policy=policy,
        )

        assert result == {}

    def test_partial_field_mappings(self) -> None:
        """Should only map fields that are configured."""
        policy = {
            "form_field_mappings": {
                "patient_name": "Name",
                "patient_dob": "DOB",
            }
        }

        result = _build_field_mappings(
            patient_name="John Doe",
            patient_dob="1980-05-15",
            member_id="MEM123",
            diagnosis_codes=["M54.5"],
            procedure_code="72148",
            clinical_summary="Summary",
            policy=policy,
        )

        assert result == {"Name": "John Doe", "DOB": "1980-05-15"}

    def test_multiple_secondary_diagnoses(self, sample_policy: dict) -> None:
        """Should join multiple secondary diagnoses with comma."""
        result = _build_field_mappings(
            patient_name="John Doe",
            patient_dob="1980-05-15",
            member_id="MEM123",
            diagnosis_codes=["M54.5", "M51.16", "G89.4"],
            procedure_code="72148",
            clinical_summary="Summary",
            policy=sample_policy,
        )

        assert result["PrimaryDiagnosis"] == "M54.5"
        assert result["SecondaryDiagnosis"] == "M51.16, G89.4"


class TestGenerateFormData:
    """Tests for generate_form_data function."""

    @pytest.mark.asyncio
    async def test_returns_correct_structure(
        self,
        sample_bundle: ClinicalBundle,
        sample_policy: dict,
        evidence_all_met: list[EvidenceItem],
    ) -> None:
        """Should return PAFormResponse with correct structure."""
        with patch(
            "src.reasoning.form_generator._generate_clinical_summary",
            new_callable=AsyncMock,
            return_value="Generated clinical summary",
        ):
            result = await generate_form_data(
                sample_bundle, evidence_all_met, sample_policy
            )

        assert result.patient_name == "John Doe"
        assert result.patient_dob == "1980-05-15"
        assert result.member_id == "MEM123456"
        assert result.diagnosis_codes == ["M54.5"]
        assert result.procedure_code == "72148"
        assert result.clinical_summary == "Generated clinical summary"
        assert result.supporting_evidence == evidence_all_met
        assert result.recommendation == "APPROVE"
        assert result.confidence_score >= 0.8
        assert "PatientFullName" in result.field_mappings

    @pytest.mark.asyncio
    async def test_missing_patient_info(
        self, sample_policy: dict, evidence_all_met: list[EvidenceItem]
    ) -> None:
        """Should handle missing patient information gracefully."""
        bundle = ClinicalBundle(patient_id="test-002", patient=None, conditions=[])

        with patch(
            "src.reasoning.form_generator._generate_clinical_summary",
            new_callable=AsyncMock,
            return_value="Summary",
        ):
            result = await generate_form_data(bundle, evidence_all_met, sample_policy)

        assert result.patient_name == "Unknown"
        assert result.patient_dob == "Unknown"
        assert result.member_id == "Unknown"
        assert result.diagnosis_codes == ["Unknown"]

    @pytest.mark.asyncio
    async def test_patient_without_birth_date(
        self, sample_policy: dict, evidence_all_met: list[EvidenceItem]
    ) -> None:
        """Should handle patient without birth date."""
        bundle = ClinicalBundle(
            patient_id="test-003",
            patient=PatientInfo(name="Jane Smith", birth_date=None, member_id=None),
            conditions=[],
        )

        with patch(
            "src.reasoning.form_generator._generate_clinical_summary",
            new_callable=AsyncMock,
            return_value="Summary",
        ):
            result = await generate_form_data(bundle, evidence_all_met, sample_policy)

        assert result.patient_name == "Jane Smith"
        assert result.patient_dob == "Unknown"
        assert result.member_id == "Unknown"

    @pytest.mark.asyncio
    async def test_empty_conditions_list(
        self, sample_policy: dict, evidence_all_met: list[EvidenceItem]
    ) -> None:
        """Should default to Unknown when no conditions."""
        bundle = ClinicalBundle(
            patient_id="test-004",
            patient=PatientInfo(name="Test Patient"),
            conditions=[],
        )

        with patch(
            "src.reasoning.form_generator._generate_clinical_summary",
            new_callable=AsyncMock,
            return_value="Summary",
        ):
            result = await generate_form_data(bundle, evidence_all_met, sample_policy)

        assert result.diagnosis_codes == ["Unknown"]

    @pytest.mark.asyncio
    async def test_uses_default_procedure_code(
        self, sample_bundle: ClinicalBundle, evidence_all_met: list[EvidenceItem]
    ) -> None:
        """Should use default procedure code when not in policy."""
        policy: dict = {"criteria": [], "form_field_mappings": {}}

        with patch(
            "src.reasoning.form_generator._generate_clinical_summary",
            new_callable=AsyncMock,
            return_value="Summary",
        ):
            result = await generate_form_data(sample_bundle, evidence_all_met, policy)

        assert result.procedure_code == "72148"

    @pytest.mark.asyncio
    async def test_includes_field_mappings(
        self,
        sample_bundle: ClinicalBundle,
        sample_policy: dict,
        evidence_all_met: list[EvidenceItem],
    ) -> None:
        """Should include field mappings in response."""
        with patch(
            "src.reasoning.form_generator._generate_clinical_summary",
            new_callable=AsyncMock,
            return_value="Clinical summary text",
        ):
            result = await generate_form_data(
                sample_bundle, evidence_all_met, sample_policy
            )

        assert result.field_mappings["PatientFullName"] == "John Doe"
        assert result.field_mappings["DateOfBirth"] == "1980-05-15"
        assert result.field_mappings["MemberID"] == "MEM123456"
        assert result.field_mappings["PrimaryDiagnosis"] == "M54.5"
        assert result.field_mappings["ProcedureCode"] == "72148"
        assert result.field_mappings["ClinicalNotes"] == "Clinical summary text"
