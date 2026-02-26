import { PDFDocument } from 'pdf-lib';
import type { PARequest } from '@/api/graphqlService';

const TEMPLATE_URL = '/pdf-templates/tx-standard-pa-form.pdf';
const FALLBACK_URL = '/pdf-templates/tx-standard-pa-form-filled.pdf';
const FILL_TIMEOUT_MS = 15_000;

/**
 * Maps PARequest fields to the TX Standard PA Form (NOFR001) field names.
 * Field names must match the AcroForm fields in the PDF exactly.
 */
function buildFieldMappings(request: PARequest): Record<string, string> {
  const today = new Date().toLocaleDateString('en-US', {
    month: '2-digit',
    day: '2-digit',
    year: 'numeric',
  });

  const serviceDate = request.serviceDate
    ? new Date(request.serviceDate).toLocaleDateString('en-US', {
        month: '2-digit',
        day: '2-digit',
        year: 'numeric',
      })
    : today;

  return {
    // Section I — Submission Information
    'Issuer Name': request.payer ?? '',
    'Submission Date': today,

    // Section III — Patient Information
    'Patient Name': request.patient?.name ?? '',
    'Patient Date of Birth': request.patient?.dob ?? '',
    'Member or Medicaid ID Number': request.patient?.memberId ?? '',
    'Patient Phone Number': request.patient?.phone ?? '',

    // Section IV — Requesting Provider
    'Requesting Provider or Facility Name': request.provider ?? '',
    'Requesting Provider or Facility NPI Number': request.providerNpi ?? '',
    'Requesting Provider or Facility Specialty': 'Family Medicine',
    'Requesting Provider or Facility Phone Number': '(555) 867-5309',
    'Requesting Provider or Facility Fax Number': '(555) 867-5310',

    // Section IV — Service Provider (where service will be rendered)
    'Service Provider or Facility Name': 'AuthScript Imaging Center',
    'Service Provider or Facility NPI Number': '9876543210',
    'Service Provider or Facility Specialty': 'Radiology',

    // Section V — Service Details
    'Planned Service or Procedure Row 1': request.procedureName ?? '',
    'Planned Service or Procedure Code Row 1': request.procedureCode ?? '',
    'Planned Service or Procedure Start Date Row 1': serviceDate,
    'Planned Service or Procedure Diagnosis Description Row 1':
      `${request.diagnosis ?? ''} (ICD-10)`,
    'Planned Service or Procedure Diagnosis Code Row 1': request.diagnosisCode ?? '',
    'Diagnosis Description ICD Version Number': '10',

    // Section VI — Clinical Documentation
    'SECTION VI  CLINICAL DOCUMENTATION SEE INSTRUCTIONS PAGE SECTION VI':
      request.clinicalSummary ?? '',
  };
}

/**
 * Checkbox fields that should be set to "Yes" for our demo case.
 */
const CHECKBOX_FIELDS = [
  'Paitent Gender - Female', // Typo matches the actual PDF form field name
  'Review Type - Non-Urgent',
  'Request Type - Initial',
  'Outpatient',
];

/**
 * Fills the TX Standard PA Form template with PA request data.
 * Returns the filled PDF as a Blob.
 */
async function fillTemplate(request: PARequest): Promise<Blob> {
  const response = await fetch(TEMPLATE_URL);
  if (!response.ok) {
    throw new Error(`Failed to fetch PDF template: ${response.status}`);
  }

  const templateBytes = await response.arrayBuffer();
  const pdfDoc = await PDFDocument.load(templateBytes);
  const form = pdfDoc.getForm();

  // Fill text fields
  const mappings = buildFieldMappings(request);
  for (const [fieldName, value] of Object.entries(mappings)) {
    try {
      const field = form.getTextField(fieldName);
      field.setText(value);
    } catch {
      // Field may not exist in this version of the form — skip silently
    }
  }

  // Set checkbox fields
  for (const fieldName of CHECKBOX_FIELDS) {
    try {
      const field = form.getCheckBox(fieldName);
      field.check();
    } catch {
      // Skip if field doesn't exist or isn't a checkbox
    }
  }

  // Flatten form so fields appear as static text
  form.flatten();

  const filledBytes = await pdfDoc.save();
  return new Blob([filledBytes.buffer as ArrayBuffer], { type: 'application/pdf' });
}

/**
 * Generates a filled PA form PDF using the real TX NOFR001 template.
 *
 * Primary: fills the template programmatically via pdf-lib.
 * Fallback: if filling exceeds FILL_TIMEOUT_MS or fails, returns the
 * pre-filled PDF template instead.
 */
export async function generateFilledPAForm(request: PARequest): Promise<Blob> {
  try {
    const fillPromise = fillTemplate(request);
    const timeoutPromise = new Promise<never>((_, reject) =>
      setTimeout(() => reject(new Error('PDF fill timeout')), FILL_TIMEOUT_MS),
    );

    return await Promise.race([fillPromise, timeoutPromise]);
  } catch {
    // Fallback to pre-filled template
    const fallbackResponse = await fetch(FALLBACK_URL);
    if (!fallbackResponse.ok) {
      throw new Error(`Failed to fetch fallback PDF: ${fallbackResponse.status}`);
    }
    return fallbackResponse.blob();
  }
}
