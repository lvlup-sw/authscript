import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

// Mock motion to avoid animation issues in tests
vi.mock('motion/react', () => ({
  motion: {
    div: ({ children, ...props }: any) => <div {...props}>{children}</div>,
    span: ({ children, ...props }: any) => <span {...props}>{children}</span>,
  },
  AnimatePresence: ({ children }: any) => <>{children}</>,
}));

import { CaseTimeline } from '../CaseTimeline';

const mockPhases = [
  {
    name: 'Submitted',
    status: 'completed' as const,
    timestamp: '2:34 PM',
    duration: '0.8s',
  },
  {
    name: 'Analyzing',
    status: 'completed' as const,
    timestamp: '2:34 PM',
    duration: '4.2s',
  },
  {
    name: 'Review Ready',
    status: 'active' as const,
    timestamp: '2:35 PM',
  },
  {
    name: 'Payer Submission',
    status: 'pending' as const,
  },
  {
    name: 'Decision',
    status: 'pending' as const,
  },
];

describe('CaseTimeline', () => {
  it('CaseTimeline_RendersPastPhases_WithCheckmarks', () => {
    render(<CaseTimeline phases={mockPhases} />);
    // Completed phases should have checkmarks
    const checkmarks = screen.getAllByTestId('phase-check');
    expect(checkmarks.length).toBe(2); // "Submitted" and "Analyzing"
  });

  it('CaseTimeline_RendersCurrentPhase_WithActiveStyle', () => {
    render(<CaseTimeline phases={mockPhases} />);
    const activePhase = screen.getByTestId('phase-active');
    expect(activePhase).toBeInTheDocument();
    // Active phase name should be in bold or have distinct styling
    expect(screen.getByText('Review Ready')).toBeInTheDocument();
  });

  it('CaseTimeline_RendersFuturePhases_AsMuted', () => {
    render(<CaseTimeline phases={mockPhases} />);
    const pendingPhases = screen.getAllByTestId('phase-pending');
    expect(pendingPhases.length).toBe(2); // "Payer Submission" and "Decision"
  });

  it('CaseTimeline_ShowsTimestamps', () => {
    render(<CaseTimeline phases={mockPhases} />);
    // Two phases share "2:34 PM", one has "2:35 PM"
    const timestamps234 = screen.getAllByText('2:34 PM');
    expect(timestamps234.length).toBe(2);
    expect(screen.getByText('2:35 PM')).toBeInTheDocument();
  });

  it('CaseTimeline_ShowsDuration', () => {
    render(<CaseTimeline phases={mockPhases} />);
    expect(screen.getByText('0.8s')).toBeInTheDocument();
    expect(screen.getByText('4.2s')).toBeInTheDocument();
  });
});
