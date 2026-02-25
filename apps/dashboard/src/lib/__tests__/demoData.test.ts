import { describe, it, expect } from 'vitest';
import { DEMO_PATIENT, DEMO_SERVICE } from '../demoData';

describe('demoData', () => {
  it('DEMO_PATIENT_Exists_HasExpectedId', () => {
    expect(DEMO_PATIENT).toBeDefined();
    expect(DEMO_PATIENT.id).toBe('60182');
  });

  it('DEMO_SERVICE_HasMRILumbarCode', () => {
    expect(DEMO_SERVICE.code).toBe('72148');
  });
});
