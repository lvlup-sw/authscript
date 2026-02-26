import { describe, it, expect } from 'vitest';
import {
  DEMO_PATIENT,
  DEMO_SERVICE,
  DEMO_EHR_PATIENT,
  DEMO_ENCOUNTER,
  DEMO_VITALS,
  DEMO_ORDERS,
  DEMO_ENCOUNTER_META,
  DEMO_PA_RESULT,
  LCD_L34220_POLICY,
} from '../demoData';

describe('demoData', () => {
  it('DEMO_PATIENT_Exists_HasExpectedId', () => {
    expect(DEMO_PATIENT).toBeDefined();
    expect(DEMO_PATIENT.id).toBe('60182');
  });

  it('DEMO_SERVICE_HasMRILumbarCode', () => {
    expect(DEMO_SERVICE.code).toBe('72148');
  });

  it('DEMO_EHR_PATIENT_MatchesIntelligenceFixture', () => {
    expect(DEMO_EHR_PATIENT.name).toBe('Rebecca Sandbox');
    expect(DEMO_EHR_PATIENT.dob).toBe('09/14/1990');
    expect(DEMO_EHR_PATIENT.mrn).toBe('ATH60182');
  });

  it('DEMO_ENCOUNTER_AgeMatchesDOB', () => {
    expect(DEMO_ENCOUNTER.hpi).toContain('35-year-old');
    expect(DEMO_ENCOUNTER.hpi).not.toContain('45-year-old');
  });

  it('DEMO_VITALS_HasRequiredFields', () => {
    expect(DEMO_VITALS.bp).toBe('128/82');
    expect(DEMO_VITALS.hr).toBe(72);
    expect(DEMO_VITALS.temp).toBe(98.6);
    expect(DEMO_VITALS.spo2).toBe(99);
  });

  it('DEMO_ORDERS_HasMRIWithPAStatus', () => {
    expect(DEMO_ORDERS).toHaveLength(1);
    expect(DEMO_ORDERS[0].code).toBe('72148');
    expect(DEMO_ORDERS[0].status).toBe('requires-pa');
  });

  it('DEMO_ENCOUNTER_META_HasProviderInfo', () => {
    expect(DEMO_ENCOUNTER_META.provider).toBe('Dr. Kelli Smith');
    expect(DEMO_ENCOUNTER_META.specialty).toBe('Family Medicine');
  });

  it('DEMO_PA_RESULT_HasAllFiveLCDCriteria', () => {
    expect(DEMO_PA_RESULT.criteria).toHaveLength(5);
    expect(DEMO_PA_RESULT.criteria.every((c) => c.met === true)).toBe(true);
  });

  it('DEMO_PA_RESULT_Has88PercentConfidence', () => {
    expect(DEMO_PA_RESULT.confidence).toBe(88);
    expect(DEMO_PA_RESULT.provider).toBe('Dr. Kelli Smith');
    expect(DEMO_PA_RESULT.procedureCode).toBe('72148');
    expect(DEMO_PA_RESULT.status).toBe('ready');
  });

  it('LCD_L34220_POLICY_HasFiveCriteriaWithRequirements', () => {
    expect(LCD_L34220_POLICY.policyId).toBe('LCD L34220');
    expect(LCD_L34220_POLICY.procedureCode).toBe('72148');
    expect(LCD_L34220_POLICY.criteria).toHaveLength(5);
    expect(LCD_L34220_POLICY.criteria.every((c) => c.requirement.length > 0)).toBe(true);
  });
});
