import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

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

// Mock motion to avoid animation issues in tests
vi.mock('motion/react', () => ({
  motion: {
    div: ({ children, ...props }: any) => <div {...props}>{children}</div>,
  },
  AnimatePresence: ({ children }: any) => <>{children}</>,
}));

import { CaseGraph, buildCaseGraphData } from '../CaseGraph';
import { DEMO_PA_RESULT } from '@/lib/demoData';

describe('buildCaseGraphData', () => {
  const { nodes, edges } = buildCaseGraphData(DEMO_PA_RESULT);

  it('CaseGraph_CreatesPatientNode', () => {
    const patientNodes = nodes.filter((n) => n.type === 'patient');
    expect(patientNodes).toHaveLength(1);
  });

  it('CaseGraph_CreatesEvidenceNodes', () => {
    const evidenceNodes = nodes.filter((n) => n.type === 'evidence');
    expect(evidenceNodes.length).toBeGreaterThan(0);
  });

  it('CaseGraph_CreatesCriteriaNodes', () => {
    const criteriaNodes = nodes.filter((n) => n.type === 'criteria');
    expect(criteriaNodes).toHaveLength(DEMO_PA_RESULT.criteria.length);
  });

  it('CaseGraph_CreatesDecisionNode', () => {
    const decisionNodes = nodes.filter((n) => n.type === 'decision');
    expect(decisionNodes).toHaveLength(1);
  });

  it('CaseGraph_CreatesEdges_EvidenceToCriteria', () => {
    const evidenceNodes = nodes.filter((n) => n.type === 'evidence');
    const criteriaNodes = nodes.filter((n) => n.type === 'criteria');
    const evidenceToCriteriaEdges = edges.filter(
      (e) =>
        evidenceNodes.some((n) => n.id === e.source) &&
        criteriaNodes.some((n) => n.id === e.target),
    );
    expect(evidenceToCriteriaEdges.length).toBeGreaterThan(0);
  });

  it('CaseGraph_CreatesEdges_CriteriaToDecision', () => {
    const criteriaNodes = nodes.filter((n) => n.type === 'criteria');
    const decisionNodes = nodes.filter((n) => n.type === 'decision');
    const criteriaToDecisionEdges = edges.filter(
      (e) =>
        criteriaNodes.some((n) => n.id === e.source) &&
        decisionNodes.some((n) => n.id === e.target),
    );
    expect(criteriaToDecisionEdges.length).toBe(criteriaNodes.length);
  });
});

describe('CaseGraph component', () => {
  it('CaseGraph_RendersReactFlow', () => {
    render(<CaseGraph paRequest={DEMO_PA_RESULT} />);
    expect(screen.getByTestId('react-flow')).toBeInTheDocument();
  });

  it('CaseGraph_PassesCorrectNodeCount', () => {
    render(<CaseGraph paRequest={DEMO_PA_RESULT} />);
    const flowEl = screen.getByTestId('react-flow');
    const nodeCount = parseInt(flowEl.getAttribute('data-nodes')!, 10);
    // patient(1) + evidence(N) + criteria(5) + decision(1) = total
    expect(nodeCount).toBeGreaterThanOrEqual(7);
  });
});
