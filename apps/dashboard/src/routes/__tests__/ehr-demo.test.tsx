import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import type { PARequest } from '@/api/graphqlService';

// Mock the GraphQL service hooks
const mockCreateMutateAsync = vi.fn();
const mockProcessMutateAsync = vi.fn();
const mockSubmitMutateAsync = vi.fn();

vi.mock('@/api/graphqlService', () => ({
  useCreatePARequest: () => ({ mutateAsync: mockCreateMutateAsync, isPending: false }),
  useProcessPARequest: () => ({ mutateAsync: mockProcessMutateAsync, isPending: false }),
  useSubmitPARequest: () => ({ mutateAsync: mockSubmitMutateAsync, isPending: false }),
}));

const MOCK_PA_REQUEST: PARequest = {
  id: 'PA-TEST',
  patientId: '60182',
  fhirPatientId: 'a-195900.E-60182',
  patient: {
    id: '60182',
    name: 'Rebecca Sandbox',
    mrn: '60182',
    dob: '09/14/1990',
    memberId: 'ATH60182',
    payer: 'Blue Cross Blue Shield',
    address: '654 Birch Road, Tacoma, WA 98402',
    phone: '(253) 555-0654',
  },
  procedureCode: '72148',
  procedureName: 'MRI without Contrast, Lumbar Spine',
  diagnosis: 'Lumbar radiculopathy',
  diagnosisCode: 'M54.16',
  payer: 'Blue Cross Blue Shield',
  provider: 'Dr. Demo',
  providerNpi: '1234567890',
  serviceDate: '2026-02-25',
  placeOfService: '11',
  clinicalSummary: 'Patient presents with chronic lower back pain...',
  status: 'ready',
  confidence: 88,
  createdAt: '2026-02-25T00:00:00Z',
  updatedAt: '2026-02-25T00:00:00Z',
  readyAt: '2026-02-25T00:00:00Z',
  submittedAt: null,
  reviewTimeSeconds: 0,
  criteria: [
    { met: true, label: 'Conservative therapy failed', reason: '8 weeks PT completed' },
    { met: true, label: 'Progressive neurological deficit', reason: 'Left foot numbness' },
  ],
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children);
}

async function renderEhrDemoPage() {
  const { EhrDemoPage } = await import('../ehr-demo');
  const Wrapper = createWrapper();
  return render(
    createElement(Wrapper, null, createElement(EhrDemoPage)),
  );
}

describe('EhrDemoPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('EhrDemoPage_Render_ShowsAllSections', async () => {
    // Arrange & Act
    await renderEhrDemoPage();

    // Assert: athenaOne header
    expect(screen.getByText('athenaOne')).toBeInTheDocument();

    // Assert: Patient name
    expect(screen.getByText('Maria Garcia')).toBeInTheDocument();

    // Assert: Encounter note sections
    expect(screen.getByText('Chief Complaint')).toBeInTheDocument();
    expect(screen.getByText(/MRI lumbar spine/i)).toBeInTheDocument();

    // Assert: Sign Encounter button visible
    expect(screen.getByRole('button', { name: 'Sign Encounter' })).toBeInTheDocument();

    // Assert: NO iframe in DOM
    expect(screen.queryByTitle('AuthScript Dashboard')).not.toBeInTheDocument();
    expect(document.querySelector('iframe')).toBeNull();

    // Assert: No PAResultsPanel visible initially
    expect(screen.queryByText('AuthScript — Prior Authorization')).not.toBeInTheDocument();
  });

  it('EhrDemoPage_Sign_ShowsProcessingPanel', async () => {
    // Arrange: Mock create to resolve with a PARequest (process will hang as a pending promise)
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    // Process never resolves so we stay in processing state
    mockProcessMutateAsync.mockReturnValue(new Promise(() => {}));

    await renderEhrDemoPage();

    // Act: Click "Sign Encounter"
    const signButton = screen.getByRole('button', { name: 'Sign Encounter' });
    fireEvent.click(signButton);

    // Assert: PAResultsPanel appears (header text)
    await waitFor(() => {
      expect(screen.getByText('AuthScript — Prior Authorization')).toBeInTheDocument();
    });

    // Assert: Button changes to "Encounter Signed"
    expect(screen.getByRole('button', { name: 'Encounter Signed' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Encounter Signed' })).toBeDisabled();
  });

  it('EhrDemoPage_Sign_OrderStatusUpdates', async () => {
    // Arrange: Mock the full flow through to reviewing
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    // Process never resolves — stays in processing/signing
    mockProcessMutateAsync.mockReturnValue(new Promise(() => {}));

    await renderEhrDemoPage();

    // Verify initial state: plan text mentions MRI
    expect(screen.getByText(/MRI lumbar spine/i)).toBeInTheDocument();

    // Act: Click sign
    fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));

    // Assert: PAResultsPanel appears (indicates flow is active)
    await waitFor(() => {
      expect(screen.getByText('AuthScript — Prior Authorization')).toBeInTheDocument();
    });
  });

  it('EhrDemoPage_FullFlow_EndToEnd', async () => {
    // Arrange: Mock the full create -> process -> reviewing flow
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    mockProcessMutateAsync.mockResolvedValue(MOCK_PA_REQUEST);

    await renderEhrDemoPage();

    // Act: Sign encounter
    fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));

    // Assert: Eventually shows confidence score from the processed PA request
    await waitFor(() => {
      expect(screen.getByText('88%')).toBeInTheDocument();
    });

    // Assert: Criteria are visible
    expect(screen.getByText('Conservative therapy failed')).toBeInTheDocument();
    expect(screen.getByText('Progressive neurological deficit')).toBeInTheDocument();

    // Assert: Submit button is visible
    expect(screen.getByRole('button', { name: /submit to blue cross/i })).toBeInTheDocument();
  });
});
