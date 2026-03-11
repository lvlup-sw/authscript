import { describe, it, expect } from 'vitest';
import {
  DEMO_PATIENT,
  DEMO_SERVICE,
  DEMO_EHR_PATIENT,
  DEMO_ENCOUNTER,
  DEMO_ENCOUNTER_BASE,
  DEMO_VITALS,
  DEMO_ORDERS,
  DEMO_ENCOUNTER_META,
  DEMO_PA_RESULT,
  DEMO_PRECHECK_CRITERIA_INITIAL,
  DEMO_PRECHECK_CRITERIA_COMPLETE,
  DEMO_PA_RESULT_SOURCES,
  LCD_L34220_POLICY,
  buildPreCheckCriteria,
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
    expect(DEMO_ENCOUNTER_META.provider).toBe('Kelli Smith, NP');
    expect(DEMO_ENCOUNTER_META.specialty).toBe('Family Medicine');
  });

  it('DEMO_PA_RESULT_HasAllFiveLCDCriteria', () => {
    expect(DEMO_PA_RESULT.criteria).toHaveLength(5);
    expect(DEMO_PA_RESULT.criteria.every((c) => c.met === true)).toBe(true);
  });

  it('DEMO_PA_RESULT_Has93PercentConfidence', () => {
    expect(DEMO_PA_RESULT.confidence).toBe(93);
    expect(DEMO_PA_RESULT.provider).toBe('Kelli Smith, NP');
    expect(DEMO_PA_RESULT.procedureCode).toBe('72148');
    expect(DEMO_PA_RESULT.status).toBe('ready');
  });

  it('DEMO_ENCOUNTER_BASE_OmitsConservativeTherapy', () => {
    expect(DEMO_ENCOUNTER_BASE.hpi).not.toContain('Failed 8 weeks');
    expect(DEMO_ENCOUNTER_BASE.hpi).not.toContain('NSAIDs');
  });

  it('DEMO_ENCOUNTER_IncludesConservativeTherapy', () => {
    expect(DEMO_ENCOUNTER.hpi).toContain('Failed 8 weeks');
    expect(DEMO_ENCOUNTER.hpi).toContain('naproxen 500mg BID');
  });

  it('DEMO_PRECHECK_CRITERIA_INITIAL_HasFourMet', () => {
    expect(DEMO_PRECHECK_CRITERIA_INITIAL).toHaveLength(5);
    const met = DEMO_PRECHECK_CRITERIA_INITIAL.filter((c) => c.status === 'met');
    expect(met).toHaveLength(4);
    const conservative = DEMO_PRECHECK_CRITERIA_INITIAL.find((c) => c.label.includes('conservative'));
    expect(conservative?.status).toBe('indeterminate');
  });

  it('DEMO_PRECHECK_CRITERIA_COMPLETE_HasFiveMet', () => {
    expect(DEMO_PRECHECK_CRITERIA_COMPLETE).toHaveLength(5);
    const met = DEMO_PRECHECK_CRITERIA_COMPLETE.filter((c) => c.status === 'met');
    expect(met).toHaveLength(5);
    met.forEach((c) => {
      expect(c.evidence).toBeTruthy();
      expect(c.source).toBeTruthy();
    });
  });

  it('buildPreCheckCriteria_DerivedFromChartData', () => {
    const criteria = buildPreCheckCriteria(DEMO_ENCOUNTER);

    // Red flag criterion sources evidence from encounter
    const redFlag = criteria.find((c) => c.label.includes('Red flag'));
    expect(redFlag?.evidence).toContain('Progressive numbness');

    // Conservative management sources from encounter HPI
    const conservative = criteria.find((c) => c.label.includes('conservative'));
    expect(conservative?.evidence).toBeTruthy();

    // Clinical rationale sources from encounter assessment
    const rationale = criteria.find((c) => c.label.includes('Clinical rationale'));
    expect(rationale?.evidence).toContain('warrant');
  });

  it('DEMO_PRECHECK_CRITERIA_LabelsMatchLCDPolicy', () => {
    const policyLabels = new Set(LCD_L34220_POLICY.criteria.map((c) => c.label));
    DEMO_PRECHECK_CRITERIA_COMPLETE.forEach((c) => {
      expect(policyLabels.has(c.label)).toBe(true);
    });
  });

  it('DEMO_PA_RESULT_CriteriaLabelsMatchLCDPolicy', () => {
    const policyLabels = new Set(LCD_L34220_POLICY.criteria.map((c) => c.label));
    DEMO_PA_RESULT.criteria.forEach((c) => {
      expect(policyLabels.has(c.label)).toBe(true);
    });
  });

  it('DEMO_PA_RESULT_SOURCES_HasEntryForEachCriterion', () => {
    DEMO_PA_RESULT.criteria.forEach((c) => {
      const source = DEMO_PA_RESULT_SOURCES[c.label];
      expect(source).toBeDefined();
      expect(source.evidence).toBeTruthy();
      expect(source.source).toBeTruthy();
    });
  });

  it('LCD_L34220_POLICY_HasFiveCriteriaWithRequirements', () => {
    expect(LCD_L34220_POLICY.policyId).toBe('LCD L34220');
    expect(LCD_L34220_POLICY.procedureCode).toBe('72148');
    expect(LCD_L34220_POLICY.criteria).toHaveLength(5);
    expect(LCD_L34220_POLICY.criteria.every((c) => c.requirement.length > 0)).toBe(true);
  });
});
