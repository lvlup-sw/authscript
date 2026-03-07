import { useState, useCallback, useRef } from 'react';
import type { PARequest } from '@/api/graphqlService';
import { DEMO_PA_RESULT, DEMO_PRECHECK_CRITERIA } from '@/lib/demoData';
import type { PreCheckCriterion } from '@/lib/demoData';

export type EhrDemoState = 'idle' | 'flagged' | 'signing' | 'processing' | 'reviewing' | 'submitting' | 'complete' | 'error';

export interface EhrDemoFlow {
  state: EhrDemoState;
  paRequest: PARequest | null;
  preCheckCriteria: PreCheckCriterion[] | null;
  error: string | null;
  flag: () => Promise<void>;
  sign: () => Promise<void>;
  submit: () => Promise<void>;
  reset: () => void;
}

/** Minimum time (ms) to stay in processing state for animation realism. */
const PROCESSING_DELAY_MS = 5_000;

/** Minimum time (ms) for the submit animation. */
const SUBMIT_DELAY_MS = 1_500;

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function useEhrDemoFlow(): EhrDemoFlow {
  const [state, setState] = useState<EhrDemoState>('idle');
  const [paRequest, setPaRequest] = useState<PARequest | null>(null);
  const [preCheckCriteria, setPreCheckCriteria] = useState<PreCheckCriterion[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const cancelledRef = useRef(false);

  const flag = useCallback(async () => {
    if (state !== 'idle') return;
    cancelledRef.current = false;
    await delay(1500);
    if (cancelledRef.current) return;
    setState('flagged');
    setPreCheckCriteria(DEMO_PRECHECK_CRITERIA);
  }, [state]);

  const sign = useCallback(async () => {
    try {
      setState('signing');

      // Brief signing phase before processing animation starts
      await delay(800);
      setState('processing');

      // Simulated processing — gives the animation time to complete all steps
      await delay(PROCESSING_DELAY_MS);

      // Surface the pre-built demo result
      const result: PARequest = {
        ...DEMO_PA_RESULT,
        id: `PA-DEMO-${Date.now()}`,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        readyAt: new Date().toISOString(),
      };

      setState('reviewing');
      setPaRequest(result);
    } catch (err) {
      setState('error');
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  }, []);

  const submit = useCallback(async () => {
    if (!paRequest) return;

    try {
      setState('submitting');
      await delay(SUBMIT_DELAY_MS);

      setPaRequest({
        ...paRequest,
        status: 'waiting_for_insurance',
        submittedAt: new Date().toISOString(),
      });
      setState('complete');
    } catch (err) {
      setState('error');
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  }, [paRequest]);

  const reset = useCallback(() => {
    cancelledRef.current = true;
    setState('idle');
    setPaRequest(null);
    setPreCheckCriteria(null);
    setError(null);
  }, []);

  return { state, paRequest, preCheckCriteria, error, flag, sign, submit, reset };
}
