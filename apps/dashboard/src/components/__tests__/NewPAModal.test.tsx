import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { NewPAModal } from '../NewPAModal';
import { type Patient } from '../../lib/patients';

// Mock TanStack Router
vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => vi.fn(),
}));

// Mock GraphQL hooks
vi.mock('@/api/graphqlService', () => ({
  useProcedures: () => ({ data: [] }),
  useMedications: () => ({ data: [] }),
  useCreatePARequest: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useProcessPARequest: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

// Mock createPortal to render children inline instead of into document.body
vi.mock('react-dom', async () => {
  const actual = await vi.importActual<typeof import('react-dom')>('react-dom');
  return {
    ...actual,
    createPortal: (children: React.ReactNode) => children,
  };
});

function createQueryWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

const mockPatient: Patient = {
  id: '60182',
  patientId: '60182',
  fhirId: 'a-195900.E-60182',
  name: 'Rebecca Sandbox',
  mrn: '60182',
  dob: '09/14/1990',
  memberId: 'ATH60182',
  payer: 'Blue Cross Blue Shield',
  address: '654 Birch Road, Tacoma, WA 98402',
  phone: '(253) 555-0654',
};

const mockService = {
  code: '72148',
  name: 'MRI Lumbar Spine',
  type: 'procedure' as const,
};

describe('NewPAModal', () => {
  it('NewPAModal_InitialPatientAndService_StartsAtConfirmStep', () => {
    render(
      <NewPAModal
        isOpen={true}
        onClose={vi.fn()}
        initialPatient={mockPatient}
        initialService={mockService}
      />,
      { wrapper: createQueryWrapper() },
    );

    // Confirm step shows patient name and service name
    expect(screen.getByText('Rebecca Sandbox')).toBeInTheDocument();
    expect(screen.getByText('MRI Lumbar Spine')).toBeInTheDocument();

    // Patient search input should NOT be present
    expect(
      screen.queryByPlaceholderText('Search by name or MRN...'),
    ).not.toBeInTheDocument();
  });

  it('NewPAModal_NoInitialData_StartsAtPatientStep', () => {
    render(<NewPAModal isOpen={true} onClose={vi.fn()} />, {
      wrapper: createQueryWrapper(),
    });

    // Patient selection UI should be visible
    expect(
      screen.getByPlaceholderText('Search by name or MRN...'),
    ).toBeInTheDocument();

    // Confirm step content should NOT be visible
    expect(screen.queryByText('Review and submit')).not.toBeInTheDocument();
  });
});
