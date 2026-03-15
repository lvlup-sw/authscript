import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createElement } from 'react';

// Mock @xyflow/react to avoid jsdom layout issues
vi.mock('@xyflow/react', () => ({
  ReactFlow: ({ nodes, edges, children }: any) => (
    <div data-testid="react-flow" data-nodes={nodes.length} data-edges={edges.length}>
      {children}
    </div>
  ),
  ReactFlowProvider: ({ children }: any) => <>{children}</>,
  Background: () => null,
  Controls: () => null,
  Handle: () => null,
  Position: { Top: 'top', Bottom: 'bottom', Left: 'left', Right: 'right' },
  useNodesState: (init: any) => [init, vi.fn(), vi.fn()],
  useEdgesState: (init: any) => [init, vi.fn(), vi.fn()],
  getSmoothStepPath: () => ['M 0 0', 0, 0],
  BaseEdge: () => null,
}));

// Mock @xyflow/react styles
vi.mock('@xyflow/react/dist/style.css', () => ({}));

// Mock motion
vi.mock('motion/react', () => ({
  motion: {
    div: ({ children, ...props }: any) => <div {...props}>{children}</div>,
    span: ({ children, ...props }: any) => <span {...props}>{children}</span>,
  },
  AnimatePresence: ({ children }: any) => <>{children}</>,
}));

// Mock TanStack Router
vi.mock('@tanstack/react-router', () => ({
  createFileRoute: () => () => ({}),
  Link: ({ children, ...props }: any) => <a {...props}>{children}</a>,
  useNavigate: () => vi.fn(),
}));

async function renderCaseDetailPage() {
  const mod = await import('../case.$caseId');
  // The module exports CaseDetailPage as the component
  const Component = (mod as any).CaseDetailPage;
  return render(createElement(Component));
}

describe('CaseDetailPage', () => {
  it('CaseDetailPage_RendersSceneNav', async () => {
    await renderCaseDetailPage();
    // SceneNav should show scene navigation items
    expect(screen.getByTestId('scene-nav')).toBeInTheDocument();
  });

  it('CaseDetailPage_RendersCaseGraph', async () => {
    await renderCaseDetailPage();
    // Left panel should contain the ReactFlow graph
    expect(screen.getByTestId('react-flow')).toBeInTheDocument();
  });

  it('CaseDetailPage_RendersCaseTimeline', async () => {
    await renderCaseDetailPage();
    // Right panel should contain timeline
    expect(screen.getByTestId('case-timeline')).toBeInTheDocument();
  });

  it('CaseDetailPage_RendersPatientSummary', async () => {
    await renderCaseDetailPage();
    // Should show patient info from DEMO_PA_RESULT
    expect(screen.getByText('Rebecca Sandbox')).toBeInTheDocument();
  });
});
