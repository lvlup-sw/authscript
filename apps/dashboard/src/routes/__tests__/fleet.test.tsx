import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { createElement } from 'react';
import { generateFleetData } from '@/lib/fleetSeedData';
import { DemoProvider } from '@/components/demo/DemoProvider';

// Mock TanStack Router
vi.mock('@tanstack/react-router', () => ({
  createFileRoute: () => () => ({}),
  Link: ({ children, ...props }: { children: React.ReactNode; to?: string }) =>
    createElement('a', props, children),
  useNavigate: () => vi.fn(),
  useLocation: () => ({ pathname: '/fleet' }),
}));

// Mock motion/react for FleetCard/FleetView animations
vi.mock('motion/react', () => ({
  motion: {
    div: ({ children, ...props }: any) => {
      const {
        initial, animate: _animate, exit, transition, layout,
        whileHover, whileTap, whileFocus, whileInView,
        ...domProps
      } = props;
      return createElement('div', domProps, children);
    },
  },
  AnimatePresence: ({ children }: any) => children,
}));

// Mock motion for KPICards count-up animation
vi.mock('motion', () => ({
  animate: (_from: number, to: number, opts: any) => {
    if (opts?.onUpdate) opts.onUpdate(to);
    return { stop: () => {} };
  },
}));

// Mock GraphQL service hooks
vi.mock('@/api/graphqlService', () => ({
  usePARequests: () => ({ data: [], isLoading: false, isError: false }),
  usePAStats: () => ({
    data: {
      ready: 0,
      submitted: 0,
      waitingForInsurance: 0,
      attention: 0,
      total: 0,
    },
  }),
  useActivity: () => ({ data: [], isLoading: false }),
}));

async function renderFleetPage() {
  const { FleetPage } = await import('../fleet');
  return render(createElement(DemoProvider, null, createElement(FleetPage)));
}

describe('FleetPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('FleetPage_RendersPageTitle_CommandCenter', async () => {
    await renderFleetPage();
    expect(
      screen.getByText('Prior Authorization Command Center'),
    ).toBeInTheDocument();
  });

  it('FleetPage_RendersKPICards', async () => {
    await renderFleetPage();
    // KPI cards are rendered -- check for test IDs from KPICards component
    expect(screen.getByTestId('kpi-card-total')).toBeInTheDocument();
    expect(screen.getByTestId('kpi-card-processing')).toBeInTheDocument();
    expect(screen.getByTestId('kpi-card-ready')).toBeInTheDocument();
    expect(screen.getByTestId('kpi-card-submitted')).toBeInTheDocument();
    expect(screen.getByTestId('kpi-card-approved')).toBeInTheDocument();
    expect(screen.getByTestId('kpi-card-denied')).toBeInTheDocument();
  });

  it('FleetPage_RendersCasePipeline', async () => {
    await renderFleetPage();
    // Pipeline stages present
    expect(screen.getByText('Order Signed')).toBeInTheDocument();
    expect(screen.getByText('PA Detected')).toBeInTheDocument();
    expect(screen.getByText('Payer Response')).toBeInTheDocument();
  });

  it('FleetPage_RendersFleetView', async () => {
    await renderFleetPage();
    // Fleet seed data generates 48 cards -- check at least some exist
    const fleetData = generateFleetData();
    const firstId = fleetData[0].id;
    expect(screen.getByTestId(`fleet-card-${firstId}`)).toBeInTheDocument();
  });

  it('FleetPage_KPICardClick_FiltersFleetView', async () => {
    await renderFleetPage();

    const fleetData = generateFleetData();
    const processingIds = fleetData
      .filter((r) => r.status === 'processing')
      .map((r) => r.id);
    const readyIds = fleetData
      .filter((r) => r.status === 'ready')
      .map((r) => r.id);

    // All cards visible initially
    expect(screen.getByTestId(`fleet-card-${processingIds[0]}`)).toBeInTheDocument();
    expect(screen.getByTestId(`fleet-card-${readyIds[0]}`)).toBeInTheDocument();

    // Click "processing" KPI card
    fireEvent.click(screen.getByTestId('kpi-card-processing'));

    // Processing cards still visible
    expect(screen.getByTestId(`fleet-card-${processingIds[0]}`)).toBeInTheDocument();

    // Ready cards hidden
    expect(screen.queryByTestId(`fleet-card-${readyIds[0]}`)).not.toBeInTheDocument();
  });
});
