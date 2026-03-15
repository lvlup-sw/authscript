import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

// Mock @xyflow/react Handle since we render nodes outside ReactFlow
vi.mock('@xyflow/react', () => ({
  Handle: () => null,
  Position: { Top: 'top', Bottom: 'bottom', Left: 'left', Right: 'right' },
}));

import { PatientNode } from '../PatientNode';
import { EvidenceNode } from '../EvidenceNode';
import { CriteriaNode } from '../CriteriaNode';
import { DecisionNode } from '../DecisionNode';

// Helper to build mock node props
function mockNodeProps<T extends Record<string, unknown>>(data: T) {
  return {
    id: 'test-node',
    data,
    type: 'custom',
    selected: false,
    isConnectable: true,
    zIndex: 0,
    positionAbsoluteX: 0,
    positionAbsoluteY: 0,
  } as any;
}

describe('PatientNode', () => {
  const patientData = {
    name: 'Rebecca Sandbox',
    dob: '09/14/1990',
    mrn: '60182',
    insurance: 'Aetna',
  };

  it('PatientNode_RendersPatientName', () => {
    render(<PatientNode {...mockNodeProps(patientData)} />);
    expect(screen.getByText('Rebecca Sandbox')).toBeInTheDocument();
  });

  it('PatientNode_RendersMRN', () => {
    render(<PatientNode {...mockNodeProps(patientData)} />);
    expect(screen.getByText(/60182/)).toBeInTheDocument();
  });

  it('PatientNode_RendersInsurance', () => {
    render(<PatientNode {...mockNodeProps(patientData)} />);
    expect(screen.getByText('Aetna')).toBeInTheDocument();
  });
});

describe('EvidenceNode', () => {
  const evidenceData = {
    text: 'Progressive numbness in left foot over past 3 weeks',
    source: 'HPI',
  };

  it('EvidenceNode_RendersEvidenceText', () => {
    render(<EvidenceNode {...mockNodeProps(evidenceData)} />);
    expect(
      screen.getByText('Progressive numbness in left foot over past 3 weeks'),
    ).toBeInTheDocument();
  });

  it('EvidenceNode_RendersSourceBadge', () => {
    render(<EvidenceNode {...mockNodeProps(evidenceData)} />);
    expect(screen.getByText('HPI')).toBeInTheDocument();
  });
});

describe('CriteriaNode', () => {
  it('CriteriaNode_MetStatus_ShowsCheckIcon', () => {
    render(
      <CriteriaNode
        {...mockNodeProps({
          label: 'Valid ICD-10',
          status: 'met' as const,
        })}
      />,
    );
    expect(screen.getByTestId('criteria-icon-met')).toBeInTheDocument();
  });

  it('CriteriaNode_NotMetStatus_ShowsXIcon', () => {
    render(
      <CriteriaNode
        {...mockNodeProps({
          label: 'Conservative therapy',
          status: 'not_met' as const,
        })}
      />,
    );
    expect(screen.getByTestId('criteria-icon-not_met')).toBeInTheDocument();
  });

  it('CriteriaNode_IndeterminateStatus_ShowsQuestionIcon', () => {
    render(
      <CriteriaNode
        {...mockNodeProps({
          label: 'Clinical rationale',
          status: 'indeterminate' as const,
        })}
      />,
    );
    expect(
      screen.getByTestId('criteria-icon-indeterminate'),
    ).toBeInTheDocument();
  });
});

describe('DecisionNode', () => {
  const decisionData = {
    payer: 'Aetna',
    policyId: 'LCD L34220',
    confidence: 93,
    status: 'ready',
  };

  it('DecisionNode_RendersPayerName', () => {
    render(<DecisionNode {...mockNodeProps(decisionData)} />);
    expect(screen.getByText('Aetna')).toBeInTheDocument();
  });

  it('DecisionNode_RendersConfidenceScore', () => {
    render(<DecisionNode {...mockNodeProps(decisionData)} />);
    expect(screen.getByText('93%')).toBeInTheDocument();
  });
});
