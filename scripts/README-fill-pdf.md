# Filling PDF Forms - Jane Doe Example

This directory contains scripts to fill out the Colorado Prescription Drug Prior Authorization Request Form with mock patient data for Jane Doe.

## Jane Doe's Profile

**Patient:** Jane Doe, Age 42
**Occupation:** Marketing Director
**Location:** Denver, Colorado
**Personality:** Professional, detail-oriented, health-conscious, active lifestyle
**Condition:** Chronic migraines with aura, diagnosed 3 years ago
**Medication Needed:** Aimovig (erenumab-aooe) - CGRP inhibitor for migraine prevention

## Files

1. **`fill_pdf_form.py`** - Python script to automatically fill the PDF form
2. **`../assets/pdf-templates/jane-doe-filled-form-reference.txt`** - Text reference showing all filled information

## Installation

To run the PDF filling script, you need to install PyMuPDF:

```bash
pip install pymupdf
```

Or if you prefer pypdf:

```bash
pip install pypdf
```

## Usage

Run the script:

```bash
python3 scripts/fill_pdf_form.py
```

This will:
- Read the original PDF template from `assets/pdf-templates/co-prescription-drug-prior-authorizathion-request-form.pdf`
- Fill it with Jane Doe's information
- Save the filled PDF to `assets/pdf-templates/co-prescription-drug-prior-authorizathion-request-form-filled-jane-doe.pdf`

## Manual Filling

If you prefer to fill the form manually or the script doesn't work perfectly, refer to:
- `assets/pdf-templates/jane-doe-filled-form-reference.txt` for all the information organized by field

## Alternative Methods

If Python libraries aren't available, you can:
1. Open the PDF in Adobe Acrobat or another PDF editor
2. Use the reference document to fill each field manually
3. Or use online PDF fillers like PDFescape, PDFfiller, etc.
