import { describe, it, expect } from 'vitest';
import { generateFleetData } from '../fleetSeedData';

describe('generateFleetData', () => {
  it('fleetSeedData_Returns48Cases', () => {
    const data = generateFleetData();
    expect(data).toHaveLength(48);
  });

  it('fleetSeedData_DistributesAcrossStatuses', () => {
    const data = generateFleetData();
    const counts = data.reduce(
      (acc, r) => {
        acc[r.status] = (acc[r.status] || 0) + 1;
        return acc;
      },
      {} as Record<string, number>,
    );
    expect(counts['processing']).toBe(6);
    expect(counts['ready']).toBe(8);
    expect(counts['submitted']).toBe(15);
    expect(counts['waiting_for_insurance']).toBe(8);
    expect(counts['approved']).toBe(9);
    expect(counts['denied']).toBe(2);
  });

  it('fleetSeedData_UsesAllTestPatients', () => {
    const data = generateFleetData();
    const patientNames = new Set(data.map((r) => r.patient.name));
    expect(patientNames.size).toBe(7);
    expect(patientNames).toContain('Donna Sandbox');
    expect(patientNames).toContain('Eleana Sandbox');
    expect(patientNames).toContain('Frankie Sandbox');
    expect(patientNames).toContain('Anna Sandbox');
    expect(patientNames).toContain('Rebecca Sandbox');
    expect(patientNames).toContain('Gary Sandbox');
    expect(patientNames).toContain('Dorrie Sandbox');
  });

  it('fleetSeedData_IncludesMultiplePayers', () => {
    const data = generateFleetData();
    const payers = new Set(data.map((r) => r.payer));
    expect(payers).toContain('Aetna');
    expect(payers).toContain('United Healthcare');
    expect(payers).toContain('Cigna');
  });

  it('fleetSeedData_ConfidenceScoresInRange', () => {
    const data = generateFleetData();
    for (const request of data) {
      expect(request.confidence).toBeGreaterThanOrEqual(60);
      expect(request.confidence).toBeLessThanOrEqual(98);
    }
  });
});
