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
    expect(result.current.paRequest!.confidence).toBe(88);
    expect(result.current.paRequest!.criteria).toHaveLength(5);
    expect(result.current.paRequest!.provider).toBe('Dr. Kelli Smith');
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
    expect(labels).toContain('4+ weeks conservative management documented');
    expect(labels).toContain('No recent duplicative CT/MRI');
  });
});
