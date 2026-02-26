import { ATHENA_TEST_PATIENTS, type Patient } from './patients';
import type { PARequest, Procedure } from '@/api/graphqlService';

// Rebecca Sandbox-Test — real Athena sandbox patient for FHIR API calls
export const DEMO_PATIENT: Patient = ATHENA_TEST_PATIENTS.find(p => p.id === '60182')!;

export const DEMO_SERVICE: Procedure = {
  code: '72148',
  name: 'MRI without Contrast, Lumbar Spine',
  category: 'Radiology',
  requiresPA: true,
};

/**
 * EHR-facing patient identity (matches Intelligence fixture demo_mri_lumbar.json)
 */
export const DEMO_EHR_PATIENT = {
  name: 'Rebecca Sandbox',
  dob: '09/14/1990',
  mrn: 'ATH60182',
};

/**
 * Demo encounter clinical content
 */
export const DEMO_ENCOUNTER = {
  cc: 'Chronic lower back pain with radiation to left lower extremity, 6 months duration, worsening over past 3 weeks',
  hpi: '35-year-old female presents with persistent lumbar pain radiating to the left leg. Pain rated 7/10, worse with prolonged sitting and standing. Reports progressive numbness in left foot over past 3 weeks. Failed 8 weeks of physical therapy (2x/week) and 6 weeks of NSAIDs (naproxen 500mg BID). No improvement with conservative management. Denies bowel/bladder dysfunction, fever, or recent trauma.',
  assessment:
    'Lumbar radiculopathy, left L5-S1. Failed conservative therapy. Progressive neurological symptoms warrant advanced imaging.',
  plan: 'Order MRI lumbar spine without contrast to evaluate for disc herniation or spinal stenosis. Continue current medications pending imaging results. Follow up in 2 weeks with MRI results.',
};

/**
 * Encounter metadata — provider, specialty, date, type
 */
export const DEMO_ENCOUNTER_META = {
  provider: 'Dr. Kelli Smith',
  specialty: 'Family Medicine',
  date: '02/25/2026',
  type: 'Office Visit',
};

/**
 * Demo vitals for the encounter
 */
export const DEMO_VITALS = {
  bp: '128/82',
  hr: 72,
  temp: 98.6,
  spo2: 99,
};

/**
 * Demo orders requiring prior authorization
 */
export const DEMO_ORDERS = [
  {
    code: '72148',
    name: 'MRI Lumbar Spine w/o Contrast',
    status: 'requires-pa' as const,
  },
];

/**
 * LCD L34220 policy requirements for MRI Lumbar Spine (CPT 72148).
 * Shown in the pre-sign policy criteria modal so providers can review
 * what documentation is needed before signing.
 */
export const LCD_L34220_POLICY = {
  policyId: 'LCD L34220',
  policyName: 'MRI Lumbar Spine',
  procedureCode: '72148',
  procedureName: 'MRI without Contrast, Lumbar Spine',
  payer: 'Blue Cross Blue Shield',
  criteria: [
    {
      label: 'Valid ICD-10 for lumbar pathology',
      requirement:
        'A valid ICD-10 diagnosis code for lumbar pathology must be documented (e.g., M54.5, M54.16, M54.51, M54.4x).',
    },
    {
      label: 'Red flag symptoms or progressive neurological deficit',
      requirement:
        'Cauda equina syndrome, suspected tumor/infection, or progressive neurological deficit (e.g., new weakness, numbness, bowel/bladder dysfunction) must be documented — OR conservative therapy must have failed.',
    },
    {
      label: '4+ weeks conservative management',
      requirement:
        'At least 4 weeks of conservative management (physical therapy, medications, injections) must be documented with outcomes, unless red flag symptoms are present.',
    },
    {
      label: 'Clinical rationale documented',
      requirement:
        'Clear clinical rationale for advanced imaging must be documented, including why imaging is medically necessary for diagnosis or treatment planning.',
    },
    {
      label: 'No recent duplicative imaging',
      requirement:
        'No prior CT or MRI of the lumbar spine within the current episode of care, unless clinical change warrants repeat imaging.',
    },
  ],
};

/**
 * Pre-built PA result for the EHR demo flow.
 * Matches the Intelligence fixture (demo_mri_lumbar.json) with all 5 LCD L34220
 * criteria MET and 88% confidence. Used instead of the real pipeline which
 * queries sparse Athena sandbox data.
 */
export const DEMO_PA_RESULT: PARequest = {
  id: `PA-DEMO-${Date.now()}`,
  patientId: '60182',
  fhirPatientId: 'a-195900.E-60182',
  patient: {
    id: '60182',
    name: 'Rebecca Sandbox',
    mrn: '60182',
    dob: '09/14/1990',
    memberId: 'ATH60182',
    payer: 'Blue Cross Blue Shield',
    address: '654 Birch Road, Tacoma, WA 98402',
    phone: '(253) 555-0654',
  },
  procedureCode: '72148',
  procedureName: 'MRI without Contrast, Lumbar Spine',
  diagnosis: 'Low back pain; Lumbar radiculopathy, left',
  diagnosisCode: 'M54.5, M54.51',
  payer: 'Blue Cross Blue Shield',
  provider: 'Dr. Kelli Smith',
  providerNpi: '1234567890',
  serviceDate: '2026-03-01',
  placeOfService: 'Outpatient',
  clinicalSummary:
    'Patient presents with chronic low back pain and left-sided lumbar radiculopathy persisting for 6+ months. ' +
    'Conservative management (PT 2x/week x 8 weeks, naproxen 500mg BID x 6 weeks) has failed. ' +
    'Progressive L5 numbness in left foot over past 3 weeks. ' +
    'MRI lumbar spine w/o contrast medically necessary per LCD L34220 to evaluate disc herniation/stenosis. ' +
    'All LCD criteria met. Policy: lcd-mri-lumbar-L34220.',
  status: 'ready',
  confidence: 88,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  readyAt: new Date().toISOString(),
  submittedAt: null,
  reviewTimeSeconds: 0,
  criteria: [
    {
      met: true,
      label: 'Valid ICD-10 for lumbar pathology',
      reason:
        'M54.5 (low back pain) and M54.51 (lumbar radiculopathy, left side) documented in assessment. Both are covered diagnoses per LCD L34220 / A57206.',
    },
    {
      met: true,
      label: 'Cauda equina, tumor, infection, major neuro deficit',
      reason:
        'Progressive neurological deficit identified: numbness in left foot worsening over 3 weeks, consistent with L5-S1 nerve root compression. This constitutes an immediate MRI indication per LCD L34220.',
    },
    {
      met: true,
      label: '4+ weeks conservative management documented',
      reason:
        'Physical therapy (2x/week for 8 weeks) and NSAIDs (naproxen 500mg BID for 6 weeks) documented with no improvement. Exceeds the 4-week minimum required by LCD L34220.',
    },
    {
      met: true,
      label: 'Supporting clinical rationale documented',
      reason:
        'Clinical rationale clearly documented: persistent radiculopathy with progressive neurological symptoms despite conservative therapy. Structural pathology (disc herniation, spinal stenosis) suspected and requires imaging confirmation to guide treatment.',
    },
    {
      met: true,
      label: 'No recent duplicative CT/MRI',
      reason:
        'No prior lumbar CT or MRI found in patient record. This is the initial advanced imaging request for this episode of care.',
    },
  ],
};
