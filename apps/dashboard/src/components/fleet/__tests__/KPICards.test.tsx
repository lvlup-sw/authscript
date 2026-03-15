import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';

// Mock motion's animate to immediately call onUpdate with final value
vi.mock('motion', () => ({
  animate: (_from: number, to: number, opts: any) => {
    if (opts?.onUpdate) opts.onUpdate(to);
    return { stop: vi.fn() };
  },
}));

import { KPICards } from '../KPICards';

const mockStats = {
  total: 48,
  processing: 6,
  ready: 8,
  submitted: 15,
  approved: 9,
  denied: 2,
};

describe('KPICards', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('KPICards_RendersSixCards', () => {
    render(
      <KPICards stats={mockStats} activeFilter={null} onFilter={vi.fn()} />,
    );
    const cards = screen.getAllByTestId(/^kpi-card-/);
    expect(cards).toHaveLength(6);
  });

  it('KPICards_DisplaysValues_FromStats', () => {
    render(
      <KPICards stats={mockStats} activeFilter={null} onFilter={vi.fn()} />,
    );
    // Advance timers to flush all staggered count-up delays (max 500ms + some buffer)
    act(() => {
      vi.advanceTimersByTime(1000);
    });
    expect(screen.getByText('48')).toBeInTheDocument();
    expect(screen.getByText('6')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByText('15')).toBeInTheDocument();
    expect(screen.getByText('9')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('KPICards_ClickCard_CallsOnFilter', () => {
    const onFilter = vi.fn();
    render(
      <KPICards stats={mockStats} activeFilter={null} onFilter={onFilter} />,
    );
    fireEvent.click(screen.getByTestId('kpi-card-ready'));
    expect(onFilter).toHaveBeenCalledWith('ready');
  });

  it('KPICards_ActiveFilter_HasHighlightedBorder', () => {
    render(
      <KPICards stats={mockStats} activeFilter="ready" onFilter={vi.fn()} />,
    );
    const activeCard = screen.getByTestId('kpi-card-ready');
    expect(activeCard.className).toMatch(/border-4|ring-2/);

    const inactiveCard = screen.getByTestId('kpi-card-total');
    expect(inactiveCard.className).not.toMatch(/border-4|ring-2/);
  });
});
