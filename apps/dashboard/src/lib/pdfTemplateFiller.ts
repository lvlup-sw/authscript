import { PDFDocument } from 'pdf-lib';
import type { PARequest } from '@/api/graphqlService';

const TEMPLATE_URL = '/pdf-templates/ma-ct-cta-mri-mra-prior-auth-form.pdf';
const FILL_TIMEOUT_MS = 15_000;

/**
 * Maps PARequest fields to the MA CT/CTA/MRI/MRA PA Form field names.
 * Field names must match the AcroForm fields in the PDF exactly.
 */
function buildFieldMappings(request: PARequest): Record<string, string> {
  const serviceDate = request.serviceDate
    ? new Date(request.serviceDate).toLocaleDateString('en-US', {
        month: '2-digit',
        day: '2-digit',
        year: 'numeric',
      })
    : new Date().toLocaleDateString('en-US', {
        month: '2-digit',
        day: '2-digit',
        year: 'numeric',
      });

  return {
    // Patient Information
    'Patient Name First Last': request.patient?.name ?? '',
    'DOB': request.patient?.dob ?? '',
    'Health Plan': request.payer ?? '',
    'Member ID': request.patient?.memberId ?? '',

    // Ordering Provider
    'Physician Name First Last': request.provider ?? '',
    'Primary Specialty': 'Family Medicine',
    'NPI': request.providerNpi ?? '',
    'Phone': '(555) 867-5309',
    'Fax': '(555) 867-5310',
    'Contact Name': request.provider ?? '',

    // Facility / Service Provider
    'Facility Name': 'AuthScript Imaging Center',
    'NPI_2': '9876543210',
    'Address': '100 Medical Plaza Dr',
    'City': 'Boston',
    'State': 'MA',
    'Zip': '02115',

    // Service Details
    'Date of Service': serviceDate,
    'CPT Codes': request.procedureCode ?? '',
    'Description': request.procedureName ?? '',
    'ICD Diagnosis Codes': request.diagnosisCode ?? '',
    'Description_2': request.diagnosis ?? '',
  };
}

/**
 * Checkbox fields that should be checked for our MRI lumbar spine demo case.
 * Field names must match the AcroForm fields in the PDF exactly.
 */
const CHECKBOX_FIELDS = [
  'MRI',
  'SPINE',
  'Radiculopathy',
  'Persistent Pain',
  'NSAIDS',
  'Physical Therapy',
];

/**
 * Fills the MA CT/CTA/MRI/MRA PA Form template with PA request data.
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
 * Generates a filled PA form PDF using the MA CT/CTA/MRI/MRA template.
 *
 * Primary: fills the template programmatically via pdf-lib.
 * Fallback: if filling exceeds FILL_TIMEOUT_MS or fails, returns the
 * blank template instead.
 */
export async function generateFilledPAForm(request: PARequest): Promise<Blob> {
  try {
    const fillPromise = fillTemplate(request);
    const timeoutPromise = new Promise<never>((_, reject) =>
      setTimeout(() => reject(new Error('PDF fill timeout')), FILL_TIMEOUT_MS),
    );

    return await Promise.race([fillPromise, timeoutPromise]);
  } catch {
    // Fallback to blank template
    const fallbackResponse = await fetch(TEMPLATE_URL);
    if (!fallbackResponse.ok) {
      throw new Error(`Failed to fetch fallback PDF: ${fallbackResponse.status}`);
    }
    return fallbackResponse.blob();
  }
}
