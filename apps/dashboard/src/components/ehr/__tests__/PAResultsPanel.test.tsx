import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { PAResultsPanel } from '../PAResultsPanel';
import type { PARequest } from '@/api/graphqlService';

function buildMockPARequest(overrides: Partial<PARequest> = {}): PARequest {
  return {
    id: 'PA-TEST',
    patientId: 'P-001',
    fhirPatientId: null,
    patient: {
      id: 'P-001',
      name: 'Maria Garcia',
      mrn: '60182',
      dob: '1981-03-15',
      memberId: 'UHC-12345',
      payer: 'United Healthcare',
      address: '123 Main St',
      phone: '555-0100',
    },
    procedureCode: '72148',
    procedureName: 'MRI Lumbar Spine',
    diagnosis: 'Lumbar radiculopathy',
    diagnosisCode: 'M54.5',
    payer: 'United Healthcare',
    provider: 'Dr. Kelli Smith',
    providerNpi: '1234567890',
    serviceDate: '2026-03-01',
    placeOfService: 'Outpatient',
    clinicalSummary: 'Test summary',
    status: 'ready',
    confidence: 88,
    createdAt: '2026-02-25T10:00:00Z',
    updatedAt: '2026-02-25T10:05:00Z',
    readyAt: '2026-02-25T10:05:00Z',
    submittedAt: null,
    reviewTimeSeconds: 30,
    criteria: [
      { met: true, label: 'Valid ICD-10 code', reason: 'M54.5 is covered' },
      { met: true, label: 'Red flag positive', reason: 'Progressive deficit found' },
      { met: true, label: 'Conservative therapy', reason: 'Bypassed per LCD' },
    ],
    ...overrides,
  };
}

describe('PAResultsPanel', () => {
  it('PAResultsPanel_Processing_ShowsAnimation', () => {
    // Arrange
    render(
      <PAResultsPanel state="processing" paRequest={null} onSubmit={vi.fn()} />,
    );

    // Assert: Processing steps are visible
    expect(screen.getByText(/reading clinical notes/i)).toBeInTheDocument();
    expect(screen.getByText(/analyzing medical necessity/i)).toBeInTheDocument();
    expect(screen.getByText(/mapping to payer requirements/i)).toBeInTheDocument();
    expect(screen.getByText(/generating pa form/i)).toBeInTheDocument();

    // Assert: No confidence score visible
    expect(screen.queryByText('88%')).not.toBeInTheDocument();
  });

  it('PAResultsPanel_Reviewing_ShowsConfidenceAndCriteria', () => {
    // Arrange
    const mockPA = buildMockPARequest();

    render(
      <PAResultsPanel
        state="reviewing"
        paRequest={mockPA}
        onSubmit={vi.fn()}
      />,
    );

    // Assert: Confidence score visible
    expect(screen.getByText('88%')).toBeInTheDocument();

    // Assert: Criteria count
    expect(screen.getByText(/3\/3 met/i)).toBeInTheDocument();

    // Assert: Each criterion label visible
    expect(screen.getByText('Valid ICD-10 code')).toBeInTheDocument();
    expect(screen.getByText('Red flag positive')).toBeInTheDocument();
    expect(screen.getByText('Conservative therapy')).toBeInTheDocument();

    // Assert: Submit button visible
    expect(
      screen.getByRole('button', { name: /submit to united healthcare/i }),
    ).toBeInTheDocument();
  });

  it('PAResultsPanel_Reviewing_CriterionClick_ShowsReasonDialog', () => {
    // Arrange
    const onCriterionClick = vi.fn();
    const mockPA = buildMockPARequest();

    render(
      <PAResultsPanel
        state="reviewing"
        paRequest={mockPA}
        onSubmit={vi.fn()}
        onCriterionClick={onCriterionClick}
      />,
    );

    // Act: Click the first criterion
    fireEvent.click(screen.getByText('Valid ICD-10 code'));

    // Assert: onCriterionClick was called with the criterion
    expect(onCriterionClick).toHaveBeenCalledWith({
      met: true,
      label: 'Valid ICD-10 code',
      reason: 'M54.5 is covered',
    });
  });

  it('PAResultsPanel_Complete_ShowsSuccessState', () => {
    // Arrange
    const mockPA = buildMockPARequest({
      submittedAt: '2026-02-25T10:10:00Z',
      status: 'waiting_for_insurance',
    });

    render(
      <PAResultsPanel
        state="complete"
        paRequest={mockPA}
        onSubmit={vi.fn()}
      />,
    );

    // Assert: Success indicator visible
    expect(screen.getByText('PA Submitted')).toBeInTheDocument();

    // Assert: Submit button is NOT visible
    expect(
      screen.queryByRole('button', { name: /submit to/i }),
    ).not.toBeInTheDocument();
  });

  it('PAResultsPanel_Error_ShowsErrorMessage', () => {
    // Arrange
    render(
      <PAResultsPanel
        state="error"
        paRequest={null}
        error="Connection failed"
        onSubmit={vi.fn()}
      />,
    );

    // Assert: Error message visible
    expect(screen.getByText('Connection failed')).toBeInTheDocument();
  });
});
