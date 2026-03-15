import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { CasePipeline } from '../CasePipeline';

const STAGE_LABELS = [
  'Order Signed',
  'PA Detected',
  'Processing',
  'Ready',
  'Submitted',
  'Payer Response',
];

const mockStageCounts: Record<string, number> = {
  order_signed: 48,
  pa_detected: 42,
  processing: 6,
  ready: 8,
  submitted: 15,
  payer_response: 19,
};

describe('CasePipeline', () => {
  it('CasePipeline_RendersSixStages', () => {
    render(
      <CasePipeline
        stageCounts={mockStageCounts}
        activeStage={null}
        onFilter={vi.fn()}
      />,
    );
    for (const label of STAGE_LABELS) {
      expect(screen.getByText(label)).toBeInTheDocument();
    }
  });

  it('CasePipeline_ShowsCountPerStage', () => {
    render(
      <CasePipeline
        stageCounts={mockStageCounts}
        activeStage={null}
        onFilter={vi.fn()}
      />,
    );
    expect(screen.getByTestId('stage-count-order_signed')).toHaveTextContent('48');
    expect(screen.getByTestId('stage-count-pa_detected')).toHaveTextContent('42');
    expect(screen.getByTestId('stage-count-processing')).toHaveTextContent('6');
    expect(screen.getByTestId('stage-count-ready')).toHaveTextContent('8');
    expect(screen.getByTestId('stage-count-submitted')).toHaveTextContent('15');
    expect(screen.getByTestId('stage-count-payer_response')).toHaveTextContent('19');
  });

  it('CasePipeline_ClickStage_CallsOnFilter', () => {
    const onFilter = vi.fn();
    render(
      <CasePipeline
        stageCounts={mockStageCounts}
        activeStage={null}
        onFilter={onFilter}
      />,
    );
    fireEvent.click(screen.getByText('Processing'));
    expect(onFilter).toHaveBeenCalledWith('processing');
  });

  it('CasePipeline_ActiveStage_HasHighlight', () => {
    render(
      <CasePipeline
        stageCounts={mockStageCounts}
        activeStage="processing"
        onFilter={vi.fn()}
      />,
    );
    const activeStage = screen.getByTestId('pipeline-stage-processing');
    expect(activeStage.className).toMatch(/teal|ring/);
  });
});
