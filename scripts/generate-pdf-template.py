#!/usr/bin/env python3
"""
Generate a fillable PDF template for Prior Authorization requests.

This script creates an AcroForm PDF with fields matching those expected
by the PdfFormStamper service in the authscript-api backend.

Usage:
    python3 scripts/generate-pdf-template.py
"""

from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from reportlab.lib.colors import black, white, lightgrey
from reportlab.lib.units import inch


def create_pa_form_template(output_path: str) -> None:
    """Create a Prior Authorization form template with AcroForm fields."""
    c = canvas.Canvas(output_path, pagesize=letter)
    width, height = letter

    # Header
    c.setFont("Helvetica-Bold", 18)
    c.drawCentredString(width / 2, height - 50, "PRIOR AUTHORIZATION REQUEST")

    c.setFont("Helvetica", 10)
    c.drawCentredString(width / 2, height - 70, "MRI Lumbar Spine")

    # Draw a separator line
    c.setStrokeColor(black)
    c.line(50, height - 85, width - 50, height - 85)

    # Section: Patient Information
    y = height - 110
    c.setFont("Helvetica-Bold", 12)
    c.drawString(50, y, "PATIENT INFORMATION")
    y -= 25

    # Form fields for patient section
    patient_fields = [
        ("PatientName", "Patient Name:", 200),
        ("PatientDOB", "Date of Birth:", 150),
        ("MemberID", "Member ID:", 200),
    ]

    c.setFont("Helvetica", 10)
    for field_name, label, field_width in patient_fields:
        c.drawString(50, y, label)
        form = c.acroForm
        form.textfield(
            name=field_name,
            x=160,
            y=y - 4,
            width=field_width,
            height=18,
            borderWidth=1,
            borderColor=black,
            fillColor=white,
            textColor=black,
            fontSize=10,
        )
        y -= 28

    # Section: Clinical Information
    y -= 15
    c.setFont("Helvetica-Bold", 12)
    c.drawString(50, y, "CLINICAL INFORMATION")
    y -= 25

    clinical_fields = [
        ("PrimaryDiagnosis", "Primary Diagnosis:", 350),
        ("SecondaryDiagnosis", "Secondary Diagnosis:", 350),
        ("ProcedureCode", "Procedure Code:", 150),
        ("RequestedDateOfService", "Requested Date of Service:", 150),
    ]

    c.setFont("Helvetica", 10)
    for field_name, label, field_width in clinical_fields:
        c.drawString(50, y, label)
        form = c.acroForm
        form.textfield(
            name=field_name,
            x=200,
            y=y - 4,
            width=field_width,
            height=18,
            borderWidth=1,
            borderColor=black,
            fillColor=white,
            textColor=black,
            fontSize=10,
        )
        y -= 28

    # Clinical Justification (multiline)
    y -= 10
    c.drawString(50, y, "Clinical Justification:")
    y -= 15
    form = c.acroForm
    form.textfield(
        name="ClinicalJustification",
        x=50,
        y=y - 100,
        width=width - 100,
        height=100,
        borderWidth=1,
        borderColor=black,
        fillColor=white,
        textColor=black,
        fontSize=10,
        fieldFlags="multiline",
    )
    y -= 115

    # Section: Provider Information
    y -= 15
    c.setFont("Helvetica-Bold", 12)
    c.drawString(50, y, "PROVIDER INFORMATION")
    y -= 25

    provider_fields = [
        ("OrderingProviderName", "Provider Name:", 300),
        ("OrderingProviderNPI", "Provider NPI:", 150),
        ("FacilityName", "Facility Name:", 350),
    ]

    c.setFont("Helvetica", 10)
    for field_name, label, field_width in provider_fields:
        c.drawString(50, y, label)
        form = c.acroForm
        form.textfield(
            name=field_name,
            x=160,
            y=y - 4,
            width=field_width,
            height=18,
            borderWidth=1,
            borderColor=black,
            fillColor=white,
            textColor=black,
            fontSize=10,
        )
        y -= 28

    # Footer
    y -= 30
    c.setFont("Helvetica", 8)
    c.setFillColor(lightgrey)
    c.drawString(50, y, "This form is for demonstration purposes only.")
    c.drawString(50, y - 12, "AuthScript Prior Authorization Demo")

    c.save()
    print(f"Generated PDF template: {output_path}")


def verify_pdf_fields(pdf_path: str) -> None:
    """Verify the PDF has the expected form fields."""
    try:
        from pypdf import PdfReader

        reader = PdfReader(pdf_path)
        fields = reader.get_fields()
        if fields:
            print(f"\nVerified {len(fields)} form fields:")
            for name in sorted(fields.keys()):
                print(f"  - {name}")
        else:
            print("\nWarning: No form fields found in PDF")
    except ImportError:
        print("\nNote: pypdf not available for verification")
    except Exception as e:
        print(f"\nVerification error: {e}")


if __name__ == "__main__":
    import os

    # Determine output path relative to script location
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    output_dir = os.path.join(project_root, "assets", "pdf-templates")

    # Ensure output directory exists
    os.makedirs(output_dir, exist_ok=True)

    output_path = os.path.join(output_dir, "mri-lumbar-pa-form.pdf")
    create_pa_form_template(output_path)
    verify_pdf_fields(output_path)
