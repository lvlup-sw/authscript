import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';

describe('useEhrDemoFlow', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function importHook() {
    const mod = await import('../useEhrDemoFlow');
    return mod.useEhrDemoFlow;
  }

  /** Advance through sign() → signing (800ms) → processing (5000ms) → reviewing */
  async function advanceToReviewing(result: { current: { sign: () => Promise<void> } }) {
    let signPromise: Promise<void>;
    act(() => {
      signPromise = result.current.sign();
    });

    // 800ms → signing completes, transitions to processing
    await act(async () => {
      vi.advanceTimersByTime(800);
    });

    // 5000ms → processing completes, transitions to reviewing
    await act(async () => {
      vi.advanceTimersByTime(5000);
    });

    await act(async () => {
      await signPromise!;
    });
  }

  it('useEhrDemoFlow_InitialState_IsIdle', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    expect(result.current.state).toBe('idle');
    expect(result.current.paRequest).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.docState).toBe('idle');
  });

  it('useEhrDemoFlow_Sign_TransitionsToSigningThenProcessing', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    // Start signing (don't await — we want to observe intermediate states)
    let signPromise: Promise<void>;
    act(() => {
      signPromise = result.current.sign();
    });

    // Initially signing
    expect(result.current.state).toBe('signing');

    // After 800ms → processing
    await act(async () => {
      vi.advanceTimersByTime(800);
    });
    expect(result.current.state).toBe('processing');

    // After 5000ms more → reviewing with demo result
    await act(async () => {
      vi.advanceTimersByTime(5000);
    });

    await act(async () => {
      await signPromise;
    });

    expect(result.current.state).toBe('reviewing');
    expect(result.current.paRequest).not.toBeNull();
    expect(result.current.paRequest!.confidence).toBe(93);
    expect(result.current.paRequest!.criteria).toHaveLength(5);
    expect(result.current.paRequest!.provider).toBe('Kelli Smith, NP');
  });

  it('useEhrDemoFlow_Submit_TransitionsToComplete', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    // Get to reviewing state
    await advanceToReviewing(result);
    expect(result.current.state).toBe('reviewing');

    // Submit
    let submitPromise: Promise<void>;
    act(() => {
      submitPromise = result.current.submit();
    });
    expect(result.current.state).toBe('submitting');

    // After submit delay (1500ms)
    await act(async () => {
      vi.advanceTimersByTime(1500);
    });

    await act(async () => {
      await submitPromise!;
    });

    expect(result.current.state).toBe('complete');
    expect(result.current.paRequest!.submittedAt).not.toBeNull();
  });

  it('useEhrDemoFlow_Reset_ReturnsToIdle', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    // Get to reviewing state
    await advanceToReviewing(result);
    expect(result.current.state).toBe('reviewing');

    // Reset
    act(() => {
      result.current.reset();
    });

    expect(result.current.state).toBe('idle');
    expect(result.current.paRequest).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.docState).toBe('idle');
  });

  it('useEhrDemoFlow_Flag_TransitionsToFlagged', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    expect(result.current.state).toBe('idle');

    let flagPromise: Promise<void>;
    act(() => {
      flagPromise = result.current.flag();
    });

    // Still idle during delay
    expect(result.current.state).toBe('idle');

    // After 1500ms → flagged
    await act(async () => {
      vi.advanceTimersByTime(1500);
    });

    await act(async () => {
      await flagPromise!;
    });

    expect(result.current.state).toBe('flagged');
    expect(result.current.preCheckCriteria).not.toBeNull();
  });

  it('useEhrDemoFlow_Flagged_HasPreCheckCriteria_FourMet', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    let flagPromise: Promise<void>;
    act(() => {
      flagPromise = result.current.flag();
    });

    await act(async () => {
      vi.advanceTimersByTime(1500);
    });

    await act(async () => {
      await flagPromise!;
    });

    expect(result.current.state).toBe('flagged');
    const criteria = result.current.preCheckCriteria!;
    expect(criteria).toHaveLength(5);
    // Initial state: 4/5 met (conservative therapy is indeterminate)
    expect(criteria.filter((c) => c.status === 'met')).toHaveLength(4);
    expect(criteria.find((c) => c.label.includes('conservative'))?.status).toBe('indeterminate');
  });

  it('useEhrDemoFlow_DocFlow_OpenSuggestion', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    expect(result.current.docState).toBe('idle');

    act(() => {
      result.current.openSuggestion();
    });

    expect(result.current.docState).toBe('suggesting');
  });

  it('useEhrDemoFlow_DocFlow_InsertToNote', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    act(() => {
      result.current.openSuggestion();
    });
    expect(result.current.docState).toBe('suggesting');

    act(() => {
      result.current.insertToNote('Failed 8 weeks of physical therapy (2x/week) and 6 weeks of NSAIDs (naproxen 500mg BID). No improvement with conservative management.');
    });
    expect(result.current.docState).toBe('inserted');
    expect(result.current.encounter.hpi).toContain('Failed 8 weeks');
  });

  it('useEhrDemoFlow_DocFlow_SaveToChart_UpdatesCriteria', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    // Get to flagged state first
    let flagPromise: Promise<void>;
    act(() => {
      flagPromise = result.current.flag();
    });
    await act(async () => {
      vi.advanceTimersByTime(1500);
    });
    await act(async () => {
      await flagPromise!;
    });

    // Walk through doc flow
    act(() => {
      result.current.openSuggestion();
    });
    act(() => {
      result.current.insertToNote('Failed 8 weeks of physical therapy (2x/week) and 6 weeks of NSAIDs (naproxen 500mg BID). No improvement with conservative management.');
    });
    act(() => {
      result.current.saveToChart();
    });
    expect(result.current.docState).toBe('saving');

    // After save delay → saved + criteria updated to 5/5
    await act(async () => {
      vi.advanceTimersByTime(1800);
    });

    expect(result.current.docState).toBe('saved');
    const criteria = result.current.preCheckCriteria!;
    expect(criteria.filter((c) => c.status === 'met')).toHaveLength(5);
  });

  it('useEhrDemoFlow_Flagged_Sign_TransitionsToSigning', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    // Get to flagged state
    let flagPromise: Promise<void>;
    act(() => {
      flagPromise = result.current.flag();
    });

    await act(async () => {
      vi.advanceTimersByTime(1500);
    });

    await act(async () => {
      await flagPromise!;
    });

    expect(result.current.state).toBe('flagged');

    // Now sign from flagged state
    let signPromise: Promise<void>;
    act(() => {
      signPromise = result.current.sign();
    });

    expect(result.current.state).toBe('signing');

    // 800ms → processing
    await act(async () => {
      vi.advanceTimersByTime(800);
    });
    expect(result.current.state).toBe('processing');

    // 5000ms → reviewing
    await act(async () => {
      vi.advanceTimersByTime(5000);
    });

    await act(async () => {
      await signPromise!;
    });

    expect(result.current.state).toBe('reviewing');
    expect(result.current.paRequest).not.toBeNull();
  });

  it('useEhrDemoFlow_Reset_ClearsPreCheckCriteria', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    // Get to flagged state
    let flagPromise: Promise<void>;
    act(() => {
      flagPromise = result.current.flag();
    });

    await act(async () => {
      vi.advanceTimersByTime(1500);
    });

    await act(async () => {
      await flagPromise!;
    });

    expect(result.current.state).toBe('flagged');
    expect(result.current.preCheckCriteria).not.toBeNull();

    // Reset
    act(() => {
      result.current.reset();
    });

    expect(result.current.state).toBe('idle');
    expect(result.current.preCheckCriteria).toBeNull();
  });

  it('useEhrDemoFlow_DemoResult_HasAllFiveCriteria', async () => {
    const useEhrDemoFlow = await importHook();
    const { result } = renderHook(() => useEhrDemoFlow());

    await advanceToReviewing(result);

    const criteria = result.current.paRequest!.criteria;
    expect(criteria).toHaveLength(5);
    expect(criteria.every((c) => c.met === true)).toBe(true);

    // Verify LCD L34220 criterion labels
    const labels = criteria.map((c) => c.label);
    expect(labels).toContain('Valid ICD-10 for lumbar pathology');
    expect(labels).toContain('4+ weeks conservative management');
    expect(labels).toContain('No recent duplicative imaging');
  });
});
