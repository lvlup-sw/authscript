import { useState, useCallback } from 'react';
import {
  useCreatePARequest,
  useProcessPARequest,
  useSubmitPARequest,
  type PARequest,
  type PatientInput,
} from '@/api/graphqlService';
import { DEMO_PATIENT, DEMO_SERVICE } from '@/lib/demoData';

export type EhrDemoState = 'idle' | 'signing' | 'processing' | 'reviewing' | 'submitting' | 'complete' | 'error';

export interface EhrDemoFlow {
  state: EhrDemoState;
  paRequest: PARequest | null;
  error: string | null;
  sign: () => Promise<void>;
  submit: () => Promise<void>;
  reset: () => void;
}

function toPatientInput(patient: typeof DEMO_PATIENT): PatientInput {
  const { id, patientId, fhirId, name, mrn, dob, memberId, payer, address, phone } = patient;
  return { id, patientId, fhirId, name, mrn, dob, memberId, payer, address, phone };
}

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : 'Unknown error';
}

export function useEhrDemoFlow(): EhrDemoFlow {
  const [state, setState] = useState<EhrDemoState>('idle');
  const [paRequest, setPaRequest] = useState<PARequest | null>(null);
  const [error, setError] = useState<string | null>(null);

  const createPA = useCreatePARequest();
  const processPA = useProcessPARequest();
  const submitPA = useSubmitPARequest();

  const sign = useCallback(async () => {
    try {
      setState('signing');
      const created = await createPA.mutateAsync({
        patient: toPatientInput(DEMO_PATIENT),
        procedureCode: DEMO_SERVICE.code,
      });

      setState('processing');
      const processed = await processPA.mutateAsync(created.id);

      setState('reviewing');
      setPaRequest(processed);
    } catch (err) {
      setState('error');
      setError(toErrorMessage(err));
    }
  }, [createPA, processPA]);

  const submit = useCallback(async () => {
    if (!paRequest) return;

    try {
      setState('submitting');
      await submitPA.mutateAsync({ id: paRequest.id });
      setState('complete');
    } catch (err) {
      setState('error');
      setError(toErrorMessage(err));
    }
  }, [submitPA, paRequest]);

  const reset = useCallback(() => {
    setState('idle');
    setPaRequest(null);
    setError(null);
  }, []);

  return { state, paRequest, error, sign, submit, reset };
}
