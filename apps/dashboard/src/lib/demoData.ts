import { ATHENA_TEST_PATIENTS, type Patient } from './patients';

// Rebecca Sandbox-Test -- MRI Lumbar with UHC
export const DEMO_PATIENT: Patient = ATHENA_TEST_PATIENTS.find(p => p.id === '60182')!;

export const DEMO_SERVICE = {
  code: '72148',
  name: 'MRI without Contrast, Lumbar Spine',
  type: 'procedure' as const,
};
