import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { createElement, useContext } from 'react';
import { DemoProvider, DemoContext } from '../DemoProvider';
import type { DemoContextValue } from '../DemoProvider';

/** Test helper that renders a consumer inside DemoProvider */
function renderWithProvider(providerProps?: { autoPlay?: boolean }) {
  let contextValue: DemoContextValue | null = null;

  function Consumer() {
    contextValue = useContext(DemoContext);
    if (!contextValue) throw new Error('DemoContext not found');
    return createElement('div', { 'data-testid': 'scene' }, contextValue.scene);
  }

  const result = render(
    createElement(DemoProvider, { ...providerProps, children: createElement(Consumer) }),
  );

  return { result, getContext: () => contextValue! };
}

describe('DemoProvider', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('DemoProvider_Default_StartsWithEncounterScene', () => {
    renderWithProvider();
    expect(screen.getByTestId('scene').textContent).toBe('encounter');
  });

  it('DemoProvider_TransitionToFleet_SetsFleetScene', () => {
    const { getContext } = renderWithProvider();

    act(() => {
      getContext().setScene('fleet');
    });

    expect(screen.getByTestId('scene').textContent).toBe('fleet');
  });

  it('DemoProvider_AutoPlay_CyclesThroughScenes', () => {
    const { getContext } = renderWithProvider({ autoPlay: true });

    // Starts at encounter
    expect(getContext().scene).toBe('encounter');

    // After 15s, should advance to fleet
    act(() => {
      vi.advanceTimersByTime(15000);
    });
    expect(getContext().scene).toBe('fleet');

    // After another 15s, should advance to case
    act(() => {
      vi.advanceTimersByTime(15000);
    });
    expect(getContext().scene).toBe('case');

    // After another 15s, should cycle back to encounter
    act(() => {
      vi.advanceTimersByTime(15000);
    });
    expect(getContext().scene).toBe('encounter');
  });

  it('DemoProvider_ResetDemo_ResetsAllState', () => {
    const { getContext } = renderWithProvider();

    // Set some state
    act(() => {
      getContext().setScene('fleet');
      getContext().setSelectedCaseId('case-123');
      getContext().setAutoPlay(true);
    });

    expect(getContext().scene).toBe('fleet');
    expect(getContext().selectedCaseId).toBe('case-123');
    expect(getContext().autoPlay).toBe(true);

    // Reset
    act(() => {
      getContext().resetDemo();
    });

    expect(getContext().scene).toBe('encounter');
    expect(getContext().selectedCaseId).toBeNull();
    expect(getContext().autoPlay).toBe(false);
  });
});
