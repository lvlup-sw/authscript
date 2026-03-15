import { useState, useCallback, useRef } from 'react';
import type { PARequest } from '@/api/graphqlService';
import type { Encounter } from './EncounterNote';
import {
  DEMO_PA_RESULT,
  DEMO_PRECHECK_CRITERIA_INITIAL,
  DEMO_PRECHECK_CRITERIA_COMPLETE,
  DEMO_ENCOUNTER_BASE,
  DEMO_ENCOUNTER,
} from '@/lib/demoData';
import type { PreCheckCriterion } from '@/lib/demoData';

export type EhrDemoState =
  | 'idle'
  | 'chart-browsing'
  | 'order-entry'
  | 'pa-detected'
  | 'documenting'
  | 'flagged'
  | 'signing'
  | 'processing'
  | 'reviewing'
  | 'submitting'
  | 'complete'
  | 'error';

/**
 * Documentation sub-state for the "add documentation" flow.
 * idle → suggesting → inserted → saving → saved
 */
export type DocState = 'idle' | 'suggesting' | 'inserted' | 'saving' | 'saved';

export interface EhrDemoFlowOptions {
  /** Starting state: 'idle' (legacy) or 'chart-browsing' (enhanced Scene 1). Default: 'idle' */
  startState?: 'idle' | 'chart-browsing';
  /** Called when transitionToFleet() is invoked after PA completion. */
  onTransition?: () => void;
}

export interface EhrDemoFlow {
  state: EhrDemoState;
  paRequest: PARequest | null;
  preCheckCriteria: PreCheckCriterion[] | null;
  encounter: Encounter;
  docState: DocState;
  /** The actual text that was inserted into the HPI (for highlighting). */
  insertedHpiText: string | null;
  error: string | null;
  flag: () => Promise<void>;
  /** Open the AI suggestion in the HPI section */
  openSuggestion: () => void;
  /** Accept the edited suggestion — insert text into encounter note */
  insertToNote: (hpiText: string) => void;
  /** Save the updated note to the EHR */
  saveToChart: () => void;
  sign: () => Promise<void>;
  submit: () => Promise<void>;
  reset: () => void;
  /** Enhanced: transition from chart-browsing to order-entry */
  addOrder: () => void;
  /** Enhanced: transition from pa-detected to documenting */
  startDocumenting: () => void;
  /** Enhanced: transition out to fleet view after PA completion */
  transitionToFleet: () => void;
}

/** Minimum time (ms) to stay in processing state for animation realism. */
const PROCESSING_DELAY_MS = 5_000;

/** Minimum time (ms) for the submit animation. */
const SUBMIT_DELAY_MS = 1_500;

/** Simulated save-to-EHR delay. */
const SAVE_TO_CHART_DELAY_MS = 1_800;

/** Delay for order-entry → pa-detected auto-transition. */
const ORDER_ENTRY_DELAY_MS = 1_200;

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function useEhrDemoFlow(options?: EhrDemoFlowOptions): EhrDemoFlow {
  const startState = options?.startState ?? 'idle';
  const onTransitionRef = useRef(options?.onTransition);
  onTransitionRef.current = options?.onTransition;

  const [state, setState] = useState<EhrDemoState>(startState);
  const [paRequest, setPaRequest] = useState<PARequest | null>(null);
  const [preCheckCriteria, setPreCheckCriteria] = useState<PreCheckCriterion[] | null>(null);
  const [encounter, setEncounter] = useState<Encounter>(DEMO_ENCOUNTER_BASE);
  const [docState, setDocState] = useState<DocState>('idle');
  const [insertedHpiText, setInsertedHpiText] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const cancelledRef = useRef(false);

  const flag = useCallback(async () => {
    // Allow flag from idle OR documenting (enhanced flow)
    if (state !== 'idle' && state !== 'documenting') return;
    cancelledRef.current = false;
    await delay(1500);
    if (cancelledRef.current) return;
    setState('flagged');
    // Start with 4/5 criteria (conservative therapy indeterminate)
    setPreCheckCriteria(DEMO_PRECHECK_CRITERIA_INITIAL);
  }, [state]);

  const openSuggestion = useCallback(() => {
    if (docState !== 'idle') return;
    setDocState('suggesting');
  }, [docState]);

  const insertToNote = useCallback((hpiText: string) => {
    if (docState !== 'suggesting') return;
    const trimmed = hpiText.trim();
    setInsertedHpiText(trimmed);
    // Build encounter with the edited text spliced into the base HPI
    setEncounter({
      ...DEMO_ENCOUNTER_BASE,
      hpi: DEMO_ENCOUNTER_BASE.hpi.replace(
        'Denies bowel/bladder dysfunction',
        `${trimmed} Denies bowel/bladder dysfunction`,
      ),
      assessment: DEMO_ENCOUNTER.assessment,
    });
    setDocState('inserted');
  }, [docState]);

  const saveToChart = useCallback(() => {
    if (docState !== 'inserted') return;
    setDocState('saving');
    setTimeout(() => {
      setDocState('saved');
      // Re-evaluate criteria after save completes
      setPreCheckCriteria(DEMO_PRECHECK_CRITERIA_COMPLETE);
    }, SAVE_TO_CHART_DELAY_MS);
  }, [docState]);

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
    setState(startState);
    setPaRequest(null);
    setPreCheckCriteria(null);
    setEncounter(DEMO_ENCOUNTER_BASE);
    setDocState('idle');
    setInsertedHpiText(null);
    setError(null);
  }, [startState]);

  // --- Enhanced flow actions ---

  const addOrder = useCallback(() => {
    if (state !== 'chart-browsing') return;
    setState('order-entry');

    // Auto-transition to pa-detected after delay
    setTimeout(() => {
      setState('pa-detected');
    }, ORDER_ENTRY_DELAY_MS);
  }, [state]);

  const startDocumenting = useCallback(() => {
    if (state !== 'pa-detected') return;
    setState('documenting');
  }, [state]);

  const transitionToFleet = useCallback(() => {
    onTransitionRef.current?.();
  }, []);

  return {
    state,
    paRequest,
    preCheckCriteria,
    encounter,
    docState,
    insertedHpiText,
    error,
    flag,
    openSuggestion,
    insertToNote,
    saveToChart,
    sign,
    submit,
    reset,
    addOrder,
    startDocumenting,
    transitionToFleet,
  };
}
