import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EncounterSidebar } from '../EncounterSidebar';
import { DEMO_CHART_DATA } from '@/lib/demoData';

describe('EncounterSidebar', () => {
  it('EncounterSidebar_Renders_AllEncounterStages', () => {
    render(<EncounterSidebar />);
    expect(screen.getByText('Review')).toBeInTheDocument();
    expect(screen.getByText('HPI')).toBeInTheDocument();
    expect(screen.getByText('ROS')).toBeInTheDocument();
    expect(screen.getByText('PE')).toBeInTheDocument();
    expect(screen.getByText('A&P')).toBeInTheDocument();
  });

  it('EncounterSidebar_NoSignOff_NotRendered', () => {
    render(<EncounterSidebar />);
    expect(screen.queryByText('Sign-Off')).not.toBeInTheDocument();
  });

  it('EncounterSidebar_ActiveStage_Highlighted', () => {
    render(<EncounterSidebar />);
    const apItem = screen.getByText('A&P').closest('[aria-current]');
    expect(apItem).toHaveAttribute('aria-current', 'step');
  });

  it('EncounterSidebar_Signed_AllEncounterStagesCompleted', () => {
    render(<EncounterSidebar signed={true} />);
    const apContainer = screen.getByText('A&P').closest('[data-stage]');
    expect(apContainer).toHaveAttribute('data-completed', 'true');
    const reviewContainer = screen.getByText('Review').closest('[data-stage]');
    expect(reviewContainer).toHaveAttribute('data-completed', 'true');
  });

  it('EncounterSidebar_Idle_NoPAStages', () => {
    render(<EncounterSidebar flowState="idle" />);
    expect(screen.queryByText('Prior Auth')).not.toBeInTheDocument();
    expect(screen.queryByText('Analyzing')).not.toBeInTheDocument();
  });

  it('EncounterSidebar_Processing_ShowsPAStages', () => {
    render(<EncounterSidebar signed={true} flowState="processing" />);
    expect(screen.getByText('Prior Auth')).toBeInTheDocument();
    expect(screen.getByText('Analyzing')).toBeInTheDocument();
    // "Review" exists in both encounter and PA sections
    expect(screen.getAllByText('Review')).toHaveLength(2);
    expect(screen.getByText('Submit')).toBeInTheDocument();
    expect(screen.getByText('Complete')).toBeInTheDocument();

    // Analyzing should be active
    const analyzingItem = screen.getByText('Analyzing').closest('[aria-current]');
    expect(analyzingItem).toHaveAttribute('aria-current', 'step');
  });

  it('EncounterSidebar_Reviewing_AnalyzingCompleted', () => {
    render(<EncounterSidebar signed={true} flowState="reviewing" />);
    const analyzingContainer = screen.getByText('Analyzing').closest('[data-stage]');
    expect(analyzingContainer).toHaveAttribute('data-completed', 'true');
  });

  it('EncounterSidebar_Flagged_ShowsPolicyCheckIndicator', () => {
    render(<EncounterSidebar flowState="flagged" preCheckCount={{ met: 5, total: 5 }} />);
    expect(screen.getByText('Policy Check')).toBeInTheDocument();
    expect(screen.getByText(/5\/5/)).toBeInTheDocument();
  });

  it('EncounterSidebar_Flagged_NoPAStages', () => {
    render(<EncounterSidebar flowState="flagged" preCheckCount={{ met: 5, total: 5 }} />);
    expect(screen.queryByText('Analyzing')).not.toBeInTheDocument();
    expect(screen.queryByText('Submit')).not.toBeInTheDocument();
    expect(screen.queryByText('Complete')).not.toBeInTheDocument();
  });

  it('EncounterSidebar_Complete_AllPAStagesCompleted', () => {
    render(<EncounterSidebar signed={true} flowState="complete" />);
    const completeContainer = screen.getByText('Complete').closest('[data-stage]');
    expect(completeContainer).toHaveAttribute('data-completed', 'true');
    const analyzingContainer = screen.getByText('Analyzing').closest('[data-stage]');
    expect(analyzingContainer).toHaveAttribute('data-completed', 'true');
  });
});

describe('EncounterSidebar Enhanced', () => {
  it('EncounterSidebar_RendersChartTabs', () => {
    render(<EncounterSidebar chartData={DEMO_CHART_DATA} />);

    expect(screen.getByRole('button', { name: /problems/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /meds/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /allergies/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /vitals/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /imaging/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /labs/i })).toBeInTheDocument();
  });

  it('EncounterSidebar_ClickTab_ShowsTabPanel', () => {
    render(<EncounterSidebar chartData={DEMO_CHART_DATA} />);

    // Click "Problems" tab
    fireEvent.click(screen.getByRole('button', { name: /problems/i }));

    // Should show ICD codes from ChartTabPanel
    expect(screen.getByText('M54.5')).toBeInTheDocument();
    expect(screen.getByText('Low back pain')).toBeInTheDocument();
  });

  it('EncounterSidebar_RendersEncounterStages', () => {
    render(<EncounterSidebar chartData={DEMO_CHART_DATA} />);

    // Existing encounter stages should still render
    expect(screen.getByText('Intake')).toBeInTheDocument();
    expect(screen.getByText('HPI')).toBeInTheDocument();
    expect(screen.getByText('ROS')).toBeInTheDocument();
    expect(screen.getByText('PE')).toBeInTheDocument();
    expect(screen.getByText('A&P')).toBeInTheDocument();
    expect(screen.getByText('Orders')).toBeInTheDocument();
    expect(screen.getByText('Sign')).toBeInTheDocument();
  });

  it('EncounterSidebar_PADetected_ShowsPAStages', () => {
    render(<EncounterSidebar chartData={DEMO_CHART_DATA} paDetected={true} />);

    // PA stages should appear after encounter stages
    expect(screen.getByText('PA Review')).toBeInTheDocument();
    expect(screen.getByText('PA Submit')).toBeInTheDocument();
  });

  it('EncounterSidebar_ActiveStage_HasTealIndicator', () => {
    render(<EncounterSidebar chartData={DEMO_CHART_DATA} activeStage="A&P" />);

    const apItem = screen.getByText('A&P').closest('[aria-current]');
    expect(apItem).toHaveAttribute('aria-current', 'step');
    // The active stage should have teal styling (via className)
    expect(apItem).toHaveClass('text-teal-700');
  });
});
