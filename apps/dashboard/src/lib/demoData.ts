import { ATHENA_TEST_PATIENTS, type Patient } from './patients';
import type { Procedure } from '@/api/graphqlService';

// Rebecca Sandbox-Test — real Athena sandbox patient for FHIR API calls
export const DEMO_PATIENT: Patient = ATHENA_TEST_PATIENTS.find(p => p.id === '60182')!;

export const DEMO_SERVICE: Procedure = {
  code: '72148',
  name: 'MRI without Contrast, Lumbar Spine',
  category: 'Radiology',
  requiresPA: true,
};

// Display-only patient for the EHR stub header (narrative patient name)
export const DEMO_EHR_PATIENT = {
  name: 'Maria Garcia',
  dob: '03/15/1981',
  mrn: 'MRN-60182',
};

export const DEMO_ENCOUNTER = {
  cc: 'Chronic lower back pain with radiation to left lower extremity, 6 months duration, worsening over past 3 weeks',
  hpi: '45-year-old female presents with persistent lumbar pain radiating to the left leg. Pain rated 7/10, worse with prolonged sitting and standing. Reports progressive numbness in left foot over past 3 weeks. Failed 8 weeks of physical therapy (2x/week) and 6 weeks of NSAIDs (naproxen 500mg BID). No improvement with conservative management. Denies bowel/bladder dysfunction, fever, or recent trauma.',
  assessment: 'Lumbar radiculopathy, left L5-S1. Failed conservative therapy. Progressive neurological symptoms warrant advanced imaging.',
  plan: 'Order MRI lumbar spine without contrast to evaluate for disc herniation or spinal stenosis. Continue current medications pending imaging results. Follow up in 2 weeks with MRI results.',
};
