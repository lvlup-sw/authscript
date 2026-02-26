import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EncounterSidebar } from '../EncounterSidebar';

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

  it('EncounterSidebar_Complete_AllPAStagesCompleted', () => {
    render(<EncounterSidebar signed={true} flowState="complete" />);
    const completeContainer = screen.getByText('Complete').closest('[data-stage]');
    expect(completeContainer).toHaveAttribute('data-completed', 'true');
    const analyzingContainer = screen.getByText('Analyzing').closest('[data-stage]');
    expect(analyzingContainer).toHaveAttribute('data-completed', 'true');
  });
});
