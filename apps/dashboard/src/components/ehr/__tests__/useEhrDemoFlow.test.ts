import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { createElement } from 'react';
import type { PARequest } from '@/api/graphqlService';
import { DEMO_PATIENT } from '@/lib/demoData';

// Mock GraphQL hooks
const mockCreateMutateAsync = vi.fn();
const mockProcessMutateAsync = vi.fn();
const mockSubmitMutateAsync = vi.fn();

vi.mock('@/api/graphqlService', () => ({
  useCreatePARequest: () => ({ mutateAsync: mockCreateMutateAsync }),
  useProcessPARequest: () => ({ mutateAsync: mockProcessMutateAsync }),
  useSubmitPARequest: () => ({ mutateAsync: mockSubmitMutateAsync }),
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children);
}

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

// Lazy import so mocks are in place before the hook module loads
async function importHook() {
  const mod = await import('../useEhrDemoFlow');
  return mod.useEhrDemoFlow;
}

describe('useEhrDemoFlow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('useEhrDemoFlow_InitialState_IsIdle', async () => {
    // Arrange
    const useEhrDemoFlow = await importHook();

    // Act
    const { result } = renderHook(() => useEhrDemoFlow(), {
      wrapper: createWrapper(),
    });

    // Assert
    expect(result.current.state).toBe('idle');
    expect(result.current.paRequest).toBeNull();
    expect(result.current.error).toBeNull();
  });

  it('useEhrDemoFlow_Sign_CallsCreateAndProcess', async () => {
    // Arrange
    const useEhrDemoFlow = await importHook();
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    mockProcessMutateAsync.mockResolvedValue(MOCK_PA_REQUEST);

    const { result } = renderHook(() => useEhrDemoFlow(), {
      wrapper: createWrapper(),
    });

    // Act
    await act(async () => {
      await result.current.sign();
    });

    // Assert — create was called with DEMO_PATIENT mapped to PatientInput + procedureCode
    expect(mockCreateMutateAsync).toHaveBeenCalledWith({
      patient: {
        id: DEMO_PATIENT.id,
        patientId: DEMO_PATIENT.patientId,
        fhirId: DEMO_PATIENT.fhirId,
        name: DEMO_PATIENT.name,
        mrn: DEMO_PATIENT.mrn,
        dob: DEMO_PATIENT.dob,
        memberId: DEMO_PATIENT.memberId,
        payer: DEMO_PATIENT.payer,
        address: DEMO_PATIENT.address,
        phone: DEMO_PATIENT.phone,
      },
      procedureCode: '72148',
    });

    // Assert — process was called with the created request's ID
    expect(mockProcessMutateAsync).toHaveBeenCalledWith(createdRequest.id);
  });

  it('useEhrDemoFlow_ProcessComplete_TransitionsToReviewing', async () => {
    // Arrange
    const useEhrDemoFlow = await importHook();
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    mockProcessMutateAsync.mockResolvedValue(MOCK_PA_REQUEST);

    const { result } = renderHook(() => useEhrDemoFlow(), {
      wrapper: createWrapper(),
    });

    // Act
    await act(async () => {
      await result.current.sign();
    });

    // Assert
    expect(result.current.state).toBe('reviewing');
    expect(result.current.paRequest).toEqual(MOCK_PA_REQUEST);
  });

  it('useEhrDemoFlow_Submit_TransitionsToComplete', async () => {
    // Arrange
    const useEhrDemoFlow = await importHook();
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    mockProcessMutateAsync.mockResolvedValue(MOCK_PA_REQUEST);
    const submittedRequest = { ...MOCK_PA_REQUEST, status: 'waiting_for_insurance' as const };
    mockSubmitMutateAsync.mockResolvedValue(submittedRequest);

    const { result } = renderHook(() => useEhrDemoFlow(), {
      wrapper: createWrapper(),
    });

    // Get to reviewing state first
    await act(async () => {
      await result.current.sign();
    });
    expect(result.current.state).toBe('reviewing');

    // Act
    await act(async () => {
      await result.current.submit();
    });

    // Assert
    expect(mockSubmitMutateAsync).toHaveBeenCalledWith({ id: MOCK_PA_REQUEST.id });
    expect(result.current.state).toBe('complete');
  });

  it('useEhrDemoFlow_ProcessError_SetsErrorState', async () => {
    // Arrange
    const useEhrDemoFlow = await importHook();
    const createdRequest = { ...MOCK_PA_REQUEST, status: 'draft' as const };
    mockCreateMutateAsync.mockResolvedValue(createdRequest);
    mockProcessMutateAsync.mockRejectedValue(new Error('Processing failed'));

    const { result } = renderHook(() => useEhrDemoFlow(), {
      wrapper: createWrapper(),
    });

    // Act
    await act(async () => {
      await result.current.sign();
    });

    // Assert
    expect(result.current.state).toBe('error');
    expect(result.current.error).toBe('Processing failed');
  });
});
