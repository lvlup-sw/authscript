#!/usr/bin/env python3
"""
Fill the Texas Standard Prior Authorization Request Form (NOFR001)
with demo data from the Intelligence service's MRI Lumbar analysis.

This demonstrates the full AuthScript pipeline:
  1. AI analyzes clinical data against LCD L34220
  2. Output maps to a real, state-mandated PA form
  3. The form can be faxed or submitted electronically to any Texas issuer

Usage:
    pip install pymupdf
    python3 scripts/fill_tx_pa_form.py

Input:  assets/pdf-templates/tx-standard-pa-form.pdf  (Texas NOFR001)
Output: assets/pdf-templates/tx-standard-pa-form-filled.pdf
"""

import json
import sys
from pathlib import Path

try:
    import fitz  # PyMuPDF
except ImportError:
    print("Error: PyMuPDF is required. Install with: pip install pymupdf")
    sys.exit(1)

PROJECT_ROOT = Path(__file__).parent.parent
FIXTURE_PATH = PROJECT_ROOT / "apps" / "intelligence" / "src" / "fixtures" / "demo_mri_lumbar.json"
INPUT_PDF = PROJECT_ROOT / "assets" / "pdf-templates" / "tx-standard-pa-form.pdf"
OUTPUT_PDF = PROJECT_ROOT / "assets" / "pdf-templates" / "tx-standard-pa-form-filled.pdf"


def load_field_mappings() -> dict[str, str]:
    """Load field mappings from the Intelligence demo fixture."""
    with open(FIXTURE_PATH) as f:
        data = json.load(f)
    return data["field_mappings"]


def fill_form(input_path: Path, output_path: Path, mappings: dict[str, str]) -> int:
    """Fill the Texas Standard PA form with mapped values. Returns count of fields filled."""
    filled = 0
    with fitz.open(str(input_path)) as doc:
        for page in doc:
            for widget in page.widgets():
                field_name = widget.field_name
                if field_name not in mappings:
                    continue
                value = mappings[field_name]
                try:
                    if widget.field_type_string == "CheckBox":
                        if value in ("Yes", "true", "True", True):
                            # Set /AS and /V directly — widget.update() doesn't reliably
                            # write the appearance state for checkboxes in all PDF viewers.
                            doc.xref_set_key(widget.xref, "AS", "/On")
                            doc.xref_set_key(widget.xref, "V", "/On")
                    else:
                        widget.field_value = str(value)
                        widget.update()
                    filled += 1
                except Exception as e:
                    print(f"  Warning: Could not update '{field_name}': {e}")

        doc.save(str(output_path), incremental=False, encryption=fitz.PDF_ENCRYPT_KEEP)
    return filled


def main() -> None:
    if not INPUT_PDF.exists():
        print(f"Error: Input PDF not found at {INPUT_PDF}")
        print("Download it from: https://www.tdi.texas.gov/forms/lhlifehealth/nofr001.pdf")
        sys.exit(1)

    if not FIXTURE_PATH.exists():
        print(f"Error: Demo fixture not found at {FIXTURE_PATH}")
        sys.exit(1)

    mappings = load_field_mappings()
    print(f"Loaded {len(mappings)} field mappings from demo fixture")
    print(f"Source: LCD L34220 — MRI Lumbar Spine analysis")
    print()

    filled = fill_form(INPUT_PDF, OUTPUT_PDF, mappings)
    print(f"Filled {filled}/{len(mappings)} fields in Texas Standard PA Form (NOFR001)")
    print(f"  Input:  {INPUT_PDF}")
    print(f"  Output: {OUTPUT_PDF}")


if __name__ == "__main__":
    main()
