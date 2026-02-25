#!/usr/bin/env python3
"""
Fill out the Colorado Prescription Drug Prior Authorization Request Form
with mock patient data for Jane Doe.

This script requires pypdf to be installed:
    pip install pypdf

Or install PyMuPDF for better text positioning:
    pip install pymupdf
"""

import sys
from pathlib import Path
from datetime import datetime

USE_PYMUPDF = False
USE_PYPDF = False

# Try to import PDF libraries
try:
    import fitz  # PyMuPDF
    USE_PYMUPDF = True
except ImportError:
    try:
        from pypdf import PdfReader, PdfWriter
        USE_PYPDF = True
    except ImportError:
        print("Error: No PDF library found. Please install one of:")
        print("  pip install pymupdf  (recommended)")
        print("  pip install pypdf")
        sys.exit(1)


# Jane Doe's Profile
# Age: 42, Marketing Director, lives in Denver, CO
# Condition: Chronic migraines with aura, diagnosed 3 years ago
# Medication: Aimovig (erenumab-aooe) - a CGRP inhibitor for migraine prevention
# Personality: Professional, detail-oriented, health-conscious, active lifestyle

PATIENT_DATA = {
    # Request Type
    "urgent": False,  # Non-urgent
    
    # Drug Information
    "drug_name": "Aimovig (erenumab-aooe)",
    "drug_scientific": "erenumab-aooe",
    "opioid_dependence": False,
    
    # Patient Information
    "patient_name": "Jane Doe",
    "member_number": "AET123456789",
    "policy_group_number": "GRP-2024-001",
    "date_of_birth": "1982-03-15",  # Age 42
    "patient_address": "2847 Elm Street, Apt 4B, Denver, CO 80202",
    "patient_phone": "(303) 555-0147",
    "patient_email": "jane.doe.email@example.com",
    
    # Prescription Date
    "prescription_date": "2026-02-20",
    
    # Prescriber Information
    "prescriber_name": "Dr. Sarah Chen, MD",
    "prescriber_fax": "(303) 555-0198",
    "prescriber_phone": "(303) 555-0197",
    "prescriber_pager": "",
    "prescriber_address": "1234 Medical Center Drive, Suite 200, Denver, CO 80218",
    "prescriber_office_contact": "Maria Rodriguez",
    "prescriber_npi": "1234567890",
    "prescriber_dea": "BC1234567",
    "prescriber_tax_id": "12-3456789",
    "specialty_facility": "Denver Neurology Associates",
    "prescriber_email": "schen@denverneuro.com",
    
    # Prior Authorization Type
    "pa_type": "New Request",  # New Request or Reauthorization
    
    # Diagnosis
    "diagnosis": "Chronic migraine with aura, intractable, with status migrainosus",
    "icd_code": "G43.109",
    
    # Drug Details
    "drug_requested": "Aimovig (erenumab-aooe)",
    "strength_route_frequency": "70 mg subcutaneous injection monthly",
    "unit_volume": "1 autoinjector pen (70 mg/1 mL)",
    "start_date": "2026-03-01",
    "length_of_therapy": "12 months",
    
    # Treatment Location
    "treatment_location": "Patient's Home - Self-administered subcutaneous injection",
    "location_npi": "",
    "location_address": "2847 Elm Street, Apt 4B, Denver, CO 80202",
    "location_tax_id": "",
    
    # Clinical Justification
    "clinical_criteria": """Patient is a 42-year-old female with a 3-year history of chronic migraines 
(15-18 headache days per month). Patient has failed multiple preventive therapies including:
- Topiramate 100mg BID for 6 months (discontinued due to cognitive side effects)
- Propranolol ER 120mg daily for 8 months (ineffective, minimal reduction in frequency)
- Amitriptyline 50mg at bedtime for 4 months (discontinued due to excessive sedation)

Patient has tried acute treatments including:
- Sumatriptan 100mg tablets (partial relief, frequent use)
- Rizatriptan 10mg (inconsistent response)

Current headache diary shows average of 16 migraine days per month over the past 3 months. 
Migraines significantly impact patient's ability to work and maintain daily activities. 
Patient is a marketing director and headaches cause frequent work absences.

Aimovig (erenumab-aooe) is a CGRP monoclonal antibody indicated for the preventive treatment 
of migraine in adults. This medication is appropriate given patient's treatment-resistant chronic 
migraine and need for a well-tolerated preventive option that does not require daily dosing.

Patient has been counseled on proper injection technique and storage requirements.""",
    
    "additional_info": "Patient has no contraindications to CGRP inhibitors. No history of cardiovascular disease.",
    "clinical_trial": False,
    
    # Prescription Details
    "dose": "70 mg",
    "route": "Subcutaneous",
    "frequency": "Monthly (once every 28 days)",
    "quantity": "1 autoinjector pen",
    "refills": "11",
    
    # Delivery
    "delivery_location": "Patient's Home",
    
    # Signature
    "signature_date": "2026-02-20",
    
    # Pharmacy
    "pharmacy_name": "HealthMart Pharmacy",
    "pharmacy_phone": "(303) 555-0200",
}


def format_date(date_str: str) -> str:
    """Convert YYYY-MM-DD to MM/DD/YYYY"""
    dt = datetime.strptime(date_str, "%Y-%m-%d")
    return dt.strftime("%m/%d/%Y")


def fill_pdf_with_pymupdf(input_pdf_path: str, output_pdf_path: str) -> None:
    """Fill PDF form fields using PyMuPDF."""
    with fitz.open(input_pdf_path) as doc:
        page = doc[0]
        widgets = list(page.widgets())

        # Track widgets that need to be updated
        widgets_to_update = []

        def set_widget_value(widget, value):
            """Set widget value and track it for update."""
            if widget:
                widget.field_value = value
                widgets_to_update.append(widget)
                return True
            return False

        def set_checkbox(widgets_list, index, checked=True):
            """Set checkbox value and track it."""
            if widgets_list and len(widgets_list) > index:
                widgets_list[index].field_value = checked
                widgets_to_update.append(widgets_list[index])
                return True
            return False

        # Urgent/Non-Urgent checkboxes (cb1) - Y=128
        # First checkbox is Urgent, second is Non-Urgent
        urgent_widgets = [w for w in widgets if w.field_name == "cb1" and abs(w.rect.y0 - 128) < 5]
        if len(urgent_widgets) >= 2:
            if not PATIENT_DATA["urgent"]:
                urgent_widgets[1].field_value = True  # Non-Urgent
                widgets_to_update.append(urgent_widgets[1])
            else:
                urgent_widgets[0].field_value = True  # Urgent
                widgets_to_update.append(urgent_widgets[0])

        # Requested Drug Name (T3) - Y=146
        drug_name_widget = next((w for w in widgets if w.field_name == "T3"), None)
        if drug_name_widget:
            drug_name_widget.field_value = PATIENT_DATA["drug_name"]
            widgets_to_update.append(drug_name_widget)

        # Opioid dependence checkboxes (cb2) - Y=165
        # First is Yes, second is No
        opioid_widgets = [w for w in widgets if w.field_name == "cb2" and abs(w.rect.y0 - 165) < 5]
        if len(opioid_widgets) >= 2:
            if not PATIENT_DATA["opioid_dependence"]:
                opioid_widgets[1].field_value = True  # No
                widgets_to_update.append(opioid_widgets[1])
            else:
                opioid_widgets[0].field_value = True  # Yes
                widgets_to_update.append(opioid_widgets[0])

        # Patient Information fields (left column) - T4 through T11
        set_widget_value(next((w for w in widgets if w.field_name == "T4"), None), PATIENT_DATA["patient_name"])
        set_widget_value(next((w for w in widgets if w.field_name == "T5"), None), PATIENT_DATA["member_number"])
        set_widget_value(next((w for w in widgets if w.field_name == "T6"), None), PATIENT_DATA["policy_group_number"])
        set_widget_value(next((w for w in widgets if w.field_name == "T7"), None), format_date(PATIENT_DATA["date_of_birth"]))
        set_widget_value(next((w for w in widgets if w.field_name == "T8"), None), PATIENT_DATA["patient_address"])
        set_widget_value(next((w for w in widgets if w.field_name == "T9"), None), PATIENT_DATA["patient_phone"])
        set_widget_value(next((w for w in widgets if w.field_name == "T10"), None), PATIENT_DATA["patient_email"])
        set_widget_value(next((w for w in widgets if w.field_name == "T11"), None), format_date(PATIENT_DATA["prescription_date"]))

        # Prescriber Information fields (right column) - T12 through T22
        set_widget_value(next((w for w in widgets if w.field_name == "T12"), None), PATIENT_DATA["prescriber_name"])
        set_widget_value(next((w for w in widgets if w.field_name == "T13"), None), PATIENT_DATA["prescriber_fax"])
        set_widget_value(next((w for w in widgets if w.field_name == "T14"), None), PATIENT_DATA["prescriber_phone"])
        if PATIENT_DATA["prescriber_pager"]:
            set_widget_value(next((w for w in widgets if w.field_name == "T15"), None), PATIENT_DATA["prescriber_pager"])
        set_widget_value(next((w for w in widgets if w.field_name == "T16"), None), PATIENT_DATA["prescriber_address"])
        set_widget_value(next((w for w in widgets if w.field_name == "T17"), None), PATIENT_DATA["prescriber_office_contact"])
        set_widget_value(next((w for w in widgets if w.field_name == "T18"), None), PATIENT_DATA["prescriber_npi"])
        set_widget_value(next((w for w in widgets if w.field_name == "T19"), None), PATIENT_DATA["prescriber_dea"])
        set_widget_value(next((w for w in widgets if w.field_name == "T20"), None), PATIENT_DATA["prescriber_tax_id"])
        set_widget_value(next((w for w in widgets if w.field_name == "T21"), None), PATIENT_DATA["specialty_facility"])
        set_widget_value(next((w for w in widgets if w.field_name == "T22"), None), PATIENT_DATA["prescriber_email"])

        # Prior Authorization Type checkboxes (cb23) - Y=437
        pa_widgets = [w for w in widgets if w.field_name == "cb23" and abs(w.rect.y0 - 437) < 5]
        if len(pa_widgets) >= 2:
            if PATIENT_DATA["pa_type"] == "New Request":
                set_checkbox(pa_widgets, 0)
            else:
                set_checkbox(pa_widgets, 1)

        # Clinical information fields
        set_widget_value(next((w for w in widgets if w.field_name == "T25"), None),
                         f"{PATIENT_DATA['diagnosis']} (ICD-10: {PATIENT_DATA['icd_code']})")
        set_widget_value(next((w for w in widgets if w.field_name == "T26"), None), PATIENT_DATA["drug_requested"])
        set_widget_value(next((w for w in widgets if w.field_name == "T27"), None), PATIENT_DATA["strength_route_frequency"])
        set_widget_value(next((w for w in widgets if w.field_name == "T28"), None), PATIENT_DATA["unit_volume"])
        set_widget_value(next((w for w in widgets if w.field_name == "T30"), None),
                         f"Start: {format_date(PATIENT_DATA['start_date'])}, Duration: {PATIENT_DATA['length_of_therapy']}")
        set_widget_value(next((w for w in widgets if w.field_name == "T29"), None), PATIENT_DATA["treatment_location"])
        set_widget_value(next((w for w in widgets if w.field_name == "T31"), None), PATIENT_DATA["clinical_criteria"])
        set_widget_value(next((w for w in widgets if w.field_name == "T32"), None), PATIENT_DATA["additional_info"])

        # Drug details
        set_widget_value(next((w for w in widgets if w.field_name == "T33.0"), None),
                         f"{PATIENT_DATA['drug_name']} {PATIENT_DATA['dose']}")
        set_widget_value(next((w for w in widgets if w.field_name == "T34"), None), PATIENT_DATA["dose"])
        set_widget_value(next((w for w in widgets if w.field_name == "T35"), None), PATIENT_DATA["route"])
        set_widget_value(next((w for w in widgets if w.field_name == "T36"), None), PATIENT_DATA["frequency"])
        set_widget_value(next((w for w in widgets if w.field_name == "T37"), None), PATIENT_DATA["quantity"])
        set_widget_value(next((w for w in widgets if w.field_name == "T38"), None), PATIENT_DATA["refills"])

        # Delivery location checkboxes (cb39, cb40, cb41) - Y=645.7
        delivery_widgets = [w for w in widgets if w.field_name in ["cb39", "cb40", "cb41"] and abs(w.rect.y0 - 645.7) < 5]
        if delivery_widgets:
            if PATIENT_DATA["delivery_location"] == "Patient's Home":
                cb39 = next((w for w in delivery_widgets if w.field_name == "cb39"), None)
                if cb39:
                    set_checkbox([cb39], 0)
            elif PATIENT_DATA["delivery_location"] == "Physician Office":
                cb40 = next((w for w in delivery_widgets if w.field_name == "cb40"), None)
                if cb40:
                    set_checkbox([cb40], 0)

        # Signature and pharmacy
        set_widget_value(next((w for w in widgets if w.field_name == "Signature3"), None), PATIENT_DATA["prescriber_name"])
        set_widget_value(next((w for w in widgets if w.field_name == "T43"), None), format_date(PATIENT_DATA["signature_date"]))
        set_widget_value(next((w for w in widgets if w.field_name == "T44"), None),
                         f"{PATIENT_DATA['pharmacy_name']}, {PATIENT_DATA['pharmacy_phone']}")

        # Update all modified widgets
        for widget in widgets_to_update:
            try:
                widget.update()
            except Exception as e:
                print(f"Warning: Could not update widget {widget.field_name}: {e}")

        # Save the filled PDF
        doc.save(output_pdf_path, incremental=False, encryption=fitz.PDF_ENCRYPT_KEEP)


def fill_pdf_with_pypdf(input_pdf_path: str, output_pdf_path: str) -> None:
    """Fill PDF using pypdf (limited - field mapping not yet implemented)."""
    reader = PdfReader(input_pdf_path)
    writer = PdfWriter()

    # Copy pages
    for page in reader.pages:
        writer.add_page(page)

    print("Warning: pypdf field mapping is not yet implemented. Output will be a copy of the original.")

    # Save
    with open(output_pdf_path, "wb") as output_file:
        writer.write(output_file)


def main():
    """Main function."""
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    input_pdf = project_root / "assets" / "pdf-templates" / "co-prescription-drug-prior-authorization-request-form.pdf"
    output_pdf = project_root / "assets" / "pdf-templates" / "co-prescription-drug-prior-authorization-request-form-filled-jane-doe.pdf"
    
    if not input_pdf.exists():
        print(f"Error: Input PDF not found at {input_pdf}")
        sys.exit(1)
    
    print(f"Filling PDF form for Jane Doe...")
    print(f"Patient: {PATIENT_DATA['patient_name']}")
    print(f"Drug: {PATIENT_DATA['drug_name']}")
    print(f"Diagnosis: {PATIENT_DATA['diagnosis']}")
    print()
    
    try:
        if USE_PYMUPDF:
            print("Using PyMuPDF...")
            fill_pdf_with_pymupdf(str(input_pdf), str(output_pdf))
        elif USE_PYPDF:
            print("Using pypdf...")
            fill_pdf_with_pypdf(str(input_pdf), str(output_pdf))
    except Exception as e:
        print(f"\n✗ Failed to fill PDF form: {e}")
        sys.exit(1)

    print(f"\n✓ Successfully filled PDF form!")
    print(f"  Input:  {input_pdf}")
    print(f"  Output: {output_pdf}")


if __name__ == "__main__":
    main()
