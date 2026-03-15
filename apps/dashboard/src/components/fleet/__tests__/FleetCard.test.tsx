import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { FleetCard } from '../FleetCard';
import type { FleetPARequest } from '@/lib/fleetSeedData';

function makeRequest(overrides: Partial<FleetPARequest> = {}): FleetPARequest {
  return {
    id: 'fleet-001',
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
    status: 'ready',
    confidence: 85,
    createdAt: '2026-03-10T08:00:00Z',
    updatedAt: '2026-03-10T10:00:00Z',
    readyAt: '2026-03-10T12:00:00Z',
    submittedAt: null,
    criteria: [
      { met: true, label: 'Valid ICD-10 diagnosis', reason: 'Covered' },
    ],
    ...overrides,
  };
}

describe('FleetCard', () => {
  it('FleetCard_RendersPatientInitials', () => {
    render(
      <FleetCard
        request={makeRequest()}
        onSelect={vi.fn()}
      />,
    );
    expect(screen.getByText('RS')).toBeInTheDocument();
  });

  it('FleetCard_RendersStatusDot_WithCorrectColor', () => {
    const { rerender } = render(
      <FleetCard
        request={makeRequest({ status: 'approved' })}
        onSelect={vi.fn()}
      />,
    );
    const approvedDot = screen.getByTestId('status-dot');
    expect(approvedDot.className).toContain('green');

    rerender(
      <FleetCard
        request={makeRequest({ status: 'denied' })}
        onSelect={vi.fn()}
      />,
    );
    const deniedDot = screen.getByTestId('status-dot');
    expect(deniedDot.className).toContain('red');

    rerender(
      <FleetCard
        request={makeRequest({ status: 'processing' })}
        onSelect={vi.fn()}
      />,
    );
    const processingDot = screen.getByTestId('status-dot');
    expect(processingDot.className).toContain('blue');
  });

  it('FleetCard_RendersProcedureCode', () => {
    render(
      <FleetCard
        request={makeRequest()}
        onSelect={vi.fn()}
      />,
    );
    expect(screen.getByText('72148')).toBeInTheDocument();
  });

  it('FleetCard_RendersPayerBadge', () => {
    render(
      <FleetCard
        request={makeRequest()}
        onSelect={vi.fn()}
      />,
    );
    expect(screen.getByText('Aetna')).toBeInTheDocument();
  });

  it('FleetCard_RendersConfidence_WhenAnalyzed', () => {
    render(
      <FleetCard
        request={makeRequest({ status: 'ready', confidence: 85 })}
        onSelect={vi.fn()}
      />,
    );
    expect(screen.getByText('85%')).toBeInTheDocument();
  });

  it('FleetCard_HighlightCase_ShowsGlow', () => {
    render(
      <FleetCard
        request={makeRequest()}
        highlighted={true}
        onSelect={vi.fn()}
      />,
    );
    const card = screen.getByTestId('fleet-card-fleet-001');
    expect(card.className).toContain('ring-2');
    expect(card.className).toContain('teal');
  });

  it('FleetCard_Click_CallsOnSelect', () => {
    const onSelect = vi.fn();
    render(
      <FleetCard
        request={makeRequest()}
        onSelect={onSelect}
      />,
    );
    fireEvent.click(screen.getByTestId('fleet-card-fleet-001'));
    expect(onSelect).toHaveBeenCalledWith('fleet-001');
  });
});
