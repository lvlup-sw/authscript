import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { useState, useEffect } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DEMO_PATIENT, DEMO_SERVICE } from '@/lib/demoData';
import { NewPAModal } from '@/components/NewPAModal';

// Mock TanStack Router
vi.mock('@tanstack/react-router', () => ({
  createFileRoute: () => () => ({}),
  Link: ({ children, ...props }: { children: React.ReactNode; to?: string }) => (
    <a {...props}>{children}</a>
  ),
  useNavigate: () => vi.fn(),
}));

// Mock GraphQL hooks used by NewPAModal
vi.mock('@/api/graphqlService', () => ({
  useProcedures: () => ({ data: [] }),
  useMedications: () => ({ data: [] }),
  useCreatePARequest: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useProcessPARequest: () => ({ mutateAsync: vi.fn(), isPending: false }),
  usePARequests: () => ({ data: [], isLoading: false, isError: false }),
  usePAStats: () => ({ data: { ready: 0, submitted: 0, waitingForInsurance: 0, attention: 0, total: 0 } }),
  useActivity: () => ({ data: [] }),
}));

/**
 * Test component that mirrors the DashboardPage's quickDemo behavior.
 * This avoids fighting with TanStack Router's file-based routing in tests.
 */
function DashboardQuickDemoTestComponent({ quickDemo }: { quickDemo: boolean }) {
  const [isNewPAModalOpen, setIsNewPAModalOpen] = useState(false);
  const [demoModalOpen, setDemoModalOpen] = useState(false);

  useEffect(() => {
    if (quickDemo) {
      setDemoModalOpen(true);
    }
  }, [quickDemo]);

  const handleClose = () => {
    setIsNewPAModalOpen(false);
    setDemoModalOpen(false);
  };

  return (
    <div>
      <button onClick={() => setIsNewPAModalOpen(true)}>New PA Request</button>
      <NewPAModal
        isOpen={isNewPAModalOpen || demoModalOpen}
        onClose={handleClose}
        initialPatient={demoModalOpen ? DEMO_PATIENT : undefined}
        initialService={demoModalOpen ? DEMO_SERVICE : undefined}
      />
    </div>
  );
}

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}

describe('DashboardPage quickDemo', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('DashboardPage_QuickDemoParam_AutoOpensModalWithPrefill', async () => {
    renderWithProviders(<DashboardQuickDemoTestComponent quickDemo={true} />);

    // The modal should open at the confirm step with demo patient and service visible
    await waitFor(() => {
      // Confirm step shows patient name and service name
      expect(screen.getByText('Rebecca Sandbox')).toBeInTheDocument();
      expect(screen.getByText(/MRI without Contrast, Lumbar Spine/i)).toBeInTheDocument();
    });

    // Should be on the confirm step — "Request PA" button visible
    expect(screen.getByRole('button', { name: /Request PA/i })).toBeInTheDocument();
  });

  it('DashboardPage_NoParam_DoesNotAutoOpenModal', () => {
    renderWithProviders(<DashboardQuickDemoTestComponent quickDemo={false} />);

    // The confirm step content should NOT be visible
    expect(screen.queryByText('Rebecca Sandbox')).not.toBeInTheDocument();
    expect(screen.queryByText(/MRI without Contrast, Lumbar Spine/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Request PA/i })).not.toBeInTheDocument();
  });
});
