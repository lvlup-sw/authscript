import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FleetView } from '../FleetView';
import type { FleetPARequest } from '@/lib/fleetSeedData';

function makeRequests(): FleetPARequest[] {
  const base = {
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
    procedureName: 'MRI Lumbar Spine without Contrast',
    diagnosis: 'Low back pain',
    diagnosisCode: 'M54.5',
    payer: 'Aetna',
    provider: 'Dr. Sarah Chen',
    providerNpi: '1234567890',
    serviceDate: '2026-03-15',
    placeOfService: 'Office',
    clinicalSummary: 'Patient presents for MRI',
    confidence: 85,
    createdAt: '2026-03-10T08:00:00Z',
    updatedAt: '2026-03-10T10:00:00Z',
    readyAt: '2026-03-10T12:00:00Z',
    submittedAt: null,
    criteria: [{ met: true, label: 'Valid ICD-10', reason: 'OK' }],
  };

  return [
    { ...base, id: 'fleet-001', status: 'ready' as const },
    { ...base, id: 'fleet-002', status: 'approved' as const },
    { ...base, id: 'fleet-003', status: 'processing' as const },
    { ...base, id: 'fleet-004', status: 'submitted' as const },
  ];
}

describe('FleetView', () => {
  it('FleetView_RendersAllCases_AsFleetCards', () => {
    render(
      <FleetView
        requests={makeRequests()}
        filter={null}
        onSelectCase={vi.fn()}
      />,
    );
    expect(screen.getByTestId('fleet-card-fleet-001')).toBeInTheDocument();
    expect(screen.getByTestId('fleet-card-fleet-002')).toBeInTheDocument();
    expect(screen.getByTestId('fleet-card-fleet-003')).toBeInTheDocument();
    expect(screen.getByTestId('fleet-card-fleet-004')).toBeInTheDocument();
  });

  it('FleetView_FilterByStatus_ShowsOnlyMatching', () => {
    render(
      <FleetView
        requests={makeRequests()}
        filter="ready"
        onSelectCase={vi.fn()}
      />,
    );
    expect(screen.getByTestId('fleet-card-fleet-001')).toBeInTheDocument();
    expect(screen.queryByTestId('fleet-card-fleet-002')).not.toBeInTheDocument();
    expect(screen.queryByTestId('fleet-card-fleet-003')).not.toBeInTheDocument();
    expect(screen.queryByTestId('fleet-card-fleet-004')).not.toBeInTheDocument();
  });

  it('FleetView_HighlightedCaseId_PassesToFleetCard', () => {
    render(
      <FleetView
        requests={makeRequests()}
        filter={null}
        highlightedCaseId="fleet-002"
        onSelectCase={vi.fn()}
      />,
    );
    const highlightedCard = screen.getByTestId('fleet-card-fleet-002');
    expect(highlightedCard.className).toContain('ring-2');
    expect(highlightedCard.className).toContain('teal');
  });
});
