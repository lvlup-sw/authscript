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
  age: 35,
  sex: 'F' as const,
  insurance: 'Aetna',
  memberId: 'ATH60182',
  allergies: ['Sulfa drugs', 'Codeine'],
};

/**
 * Demo encounter clinical content.
 * BASE version omits conservative therapy detail — used for the 4/5 "missing docs" state.
 * ENHANCED sentence is appended when the presenter "adds documentation."
 */
export const DEMO_ENCOUNTER_BASE = {
  cc: 'Chronic lower back pain with radiation to left lower extremity, 6 months duration, worsening over past 3 weeks',
  hpi: '35-year-old female presents with persistent lumbar pain radiating to the left leg. Pain rated 7/10, worse with prolonged sitting and standing. Reports progressive numbness in left foot over past 3 weeks. Denies bowel/bladder dysfunction, fever, or recent trauma.',
  assessment:
    'Lumbar radiculopathy, left L5-S1. Progressive neurological symptoms warrant advanced imaging.',
  plan: 'Order MRI lumbar spine without contrast to evaluate for disc herniation or spinal stenosis. Continue current medications pending imaging results. Follow up in 2 weeks with MRI results.',
};

/** The sentence added during the "add documentation" demo moment. */
export const DEMO_HPI_ADDENDUM =
  'Failed 8 weeks of physical therapy (2x/week) and 6 weeks of NSAIDs (naproxen 500mg BID). No improvement with conservative management.';

export const DEMO_ASSESSMENT_ADDENDUM = 'Failed conservative therapy. ';

/** Full encounter with conservative therapy documented (post-addendum). */
export const DEMO_ENCOUNTER = {
  ...DEMO_ENCOUNTER_BASE,
  hpi: DEMO_ENCOUNTER_BASE.hpi.replace(
    'Denies bowel/bladder dysfunction',
    `${DEMO_HPI_ADDENDUM} Denies bowel/bladder dysfunction`,
  ),
  assessment: DEMO_ENCOUNTER_BASE.assessment.replace(
    'Progressive neurological symptoms',
    `${DEMO_ASSESSMENT_ADDENDUM}Progressive neurological symptoms`,
  ),
};

/**
 * Encounter metadata — provider, specialty, date, type
 */
export const DEMO_ENCOUNTER_META = {
  provider: 'Kelli Smith, NP',
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
 * Pre-check criterion — used by the PAReadinessWidget before encounter signing.
 */
export interface PreCheckCriterion {
  label: string;
  status: 'met' | 'not-met' | 'indeterminate';
  evidence?: string;
  gap?: string;
  source?: string;
}

/**
 * LCD L34220 policy requirements for MRI Lumbar Spine (CPT 72148).
 * Canonical label source — all demo surfaces reference these labels.
 */
export const LCD_L34220_POLICY = {
  policyId: 'LCD L34220',
  policyName: 'MRI Lumbar Spine',
  procedureCode: '72148',
  procedureName: 'MRI without Contrast, Lumbar Spine',
  payer: 'Aetna',
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
 * Build pre-check criteria by scanning encounter data.
 * Accepts the encounter to scan — allows switching between base (4/5) and full (5/5).
 */
export function buildPreCheckCriteria(encounter: typeof DEMO_ENCOUNTER): PreCheckCriterion[] {
  const criteria: PreCheckCriterion[] = [];

  // 1. Valid ICD-10 — check known problem list
  const diagnosisCode = 'M54.5';
  criteria.push({
    label: LCD_L34220_POLICY.criteria[0].label,
    status: 'met',
    evidence: `${diagnosisCode} (low back pain) on active problem list`,
    source: 'Problem List',
  });

  // 2. Red flag / progressive neuro deficit — scan CC and HPI
  const ccMentionsProgression = encounter.cc.toLowerCase().includes('worsening');
  const hpiMentionsDeficit = encounter.hpi.toLowerCase().includes('progressive numbness');
  const hasRedFlag = ccMentionsProgression || hpiMentionsDeficit;
  const rawSnippet = hpiMentionsDeficit
    ? encounter.hpi.match(/progressive numbness[^.]*\./i)?.[0]
    : encounter.cc.match(/worsening[^,]*/i)?.[0];
  const redFlagSnippet = rawSnippet
    ? rawSnippet.charAt(0).toUpperCase() + rawSnippet.slice(1)
    : undefined;
  criteria.push({
    label: LCD_L34220_POLICY.criteria[1].label,
    status: hasRedFlag ? 'met' : 'indeterminate',
    evidence: hasRedFlag ? redFlagSnippet?.trim() : undefined,
    source: hasRedFlag ? 'CC / HPI' : undefined,
    gap: hasRedFlag ? undefined : 'No red flag symptoms identified in chart',
  });

  // 3. Conservative management — scan HPI for therapy duration
  const hpiMentionsPT = /\d+\s*weeks?\s*(of\s+)?physical therapy/i.test(encounter.hpi);
  const hpiMentionsNSAIDs = /\d+\s*weeks?\s*(of\s+)?NSAIDs/i.test(encounter.hpi);
  const hasConservative = hpiMentionsPT || hpiMentionsNSAIDs;
  const conservativeSnippet = encounter.hpi
    .match(/failed\s+\d+\s+weeks?[^.]*\./i)?.[0];
  const orderEvidence = DEMO_ORDERS.length > 0
    ? `Active order: ${DEMO_ORDERS[0].name}`
    : undefined;
  criteria.push({
    label: LCD_L34220_POLICY.criteria[2].label,
    status: hasConservative ? 'met' : 'indeterminate',
    evidence: hasConservative
      ? (conservativeSnippet?.trim() ?? orderEvidence)
      : undefined,
    source: hasConservative ? 'HPI / Orders' : undefined,
    gap: hasConservative ? undefined : 'Conservative therapy documentation not found in HPI',
  });

  // 4. Clinical rationale — scan assessment for rationale language
  const assessmentHasRationale =
    encounter.assessment.toLowerCase().includes('warrant') ||
    encounter.assessment.toLowerCase().includes('medically necessary');
  criteria.push({
    label: LCD_L34220_POLICY.criteria[3].label,
    status: assessmentHasRationale ? 'met' : 'indeterminate',
    evidence: assessmentHasRationale ? encounter.assessment : undefined,
    source: assessmentHasRationale ? 'Assessment' : undefined,
    gap: assessmentHasRationale ? undefined : 'Clinical rationale not yet documented',
  });

  // 5. No duplicative imaging — check imaging history (empty = met)
  const hasPriorImaging = false;
  criteria.push({
    label: LCD_L34220_POLICY.criteria[4].label,
    status: hasPriorImaging ? 'not-met' : 'met',
    evidence: hasPriorImaging ? undefined : 'No prior CT or MRI of lumbar spine in record',
    source: 'Imaging History',
    gap: hasPriorImaging ? 'Prior lumbar imaging found in record' : undefined,
  });

  return criteria;
}

/** Initial pre-check against base encounter (4/5 — conservative therapy missing). */
export const DEMO_PRECHECK_CRITERIA_INITIAL: PreCheckCriterion[] = buildPreCheckCriteria(DEMO_ENCOUNTER_BASE);

/** Full pre-check after documentation added (5/5 — all met). */
export const DEMO_PRECHECK_CRITERIA_COMPLETE: PreCheckCriterion[] = buildPreCheckCriteria(DEMO_ENCOUNTER);

/**
 * Source/evidence mapping for the post-sign PA result criteria.
 * Keyed by criterion label, provides extracted evidence and chart source
 * for the evidence trail display in PAResultsPanel.
 */
export const DEMO_PA_RESULT_SOURCES: Record<string, { evidence: string; source: string }> = {
  'Valid ICD-10 for lumbar pathology': {
    evidence: 'M54.5, M54.51 — low back pain, lumbar radiculopathy left',
    source: 'Assessment',
  },
  'Red flag symptoms or progressive neurological deficit': {
    evidence: 'Progressive numbness in left foot over past 3 weeks',
    source: 'HPI',
  },
  '4+ weeks conservative management': {
    evidence: '8 weeks PT (2x/week), naproxen 500mg BID x 6 weeks',
    source: 'HPI',
  },
  'Clinical rationale documented': {
    evidence: 'Persistent radiculopathy with progressive neuro symptoms despite conservative therapy',
    source: 'Assessment / Plan',
  },
  'No recent duplicative imaging': {
    evidence: 'No prior lumbar CT or MRI in patient record',
    source: 'Imaging History',
  },
};

/**
 * Pre-built PA result for the EHR demo flow.
 * Matches the Intelligence fixture (demo_mri_lumbar.json) with all 5 LCD L34220
 * criteria MET and 93% confidence (per scoring algorithm). Used instead of the real pipeline which
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
    payer: 'Aetna',
    address: '654 Birch Road, Tacoma, WA 98402',
    phone: '(253) 555-0654',
  },
  procedureCode: '72148',
  procedureName: 'MRI without Contrast, Lumbar Spine',
  diagnosis: 'Low back pain; Lumbar radiculopathy, left',
  diagnosisCode: 'M54.5, M54.51',
  payer: 'Aetna',
  provider: 'Kelli Smith, NP',
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
  confidence: 93,
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
      label: 'Red flag symptoms or progressive neurological deficit',
      reason:
        'Progressive neurological deficit identified: numbness in left foot worsening over 3 weeks, consistent with L5-S1 nerve root compression. This constitutes an immediate MRI indication per LCD L34220.',
    },
    {
      met: true,
      label: '4+ weeks conservative management',
      reason:
        'Physical therapy (2x/week for 8 weeks) and NSAIDs (naproxen 500mg BID for 6 weeks) documented with no improvement. Exceeds the 4-week minimum required by LCD L34220.',
    },
    {
      met: true,
      label: 'Clinical rationale documented',
      reason:
        'Clinical rationale clearly documented: persistent radiculopathy with progressive neurological symptoms despite conservative therapy. Structural pathology (disc herniation, spinal stenosis) suspected and requires imaging confirmation to guide treatment.',
    },
    {
      met: true,
      label: 'No recent duplicative imaging',
      reason:
        'No prior lumbar CT or MRI found in patient record. This is the initial advanced imaging request for this episode of care.',
    },
  ],
};
