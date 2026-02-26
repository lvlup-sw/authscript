import { useState, useEffect } from 'react';
import {
  ShieldCheck,
  Check,
  X,
  HelpCircle,
  ChevronRight,
  Send,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Sparkles,
  FileText,
} from 'lucide-react';
import type { EhrDemoState } from './useEhrDemoFlow';
import type { PARequest, Criterion } from '@/api/graphqlService';

export interface PAResultsPanelProps {
  state: EhrDemoState;
  paRequest: PARequest | null;
  error?: string | null;
  onSubmit: () => void;
  onCriterionClick?: (criterion: Criterion) => void;
  onViewPdf?: () => void;
}

const PROCESSING_STEPS = [
  'Reading clinical notes...',
  'Analyzing medical necessity...',
  'Mapping to payer requirements...',
  'Generating PA form...',
] as const;

function confidenceColor(confidence: number): string {
  if (confidence >= 80) return 'text-green-600';
  if (confidence >= 60) return 'text-yellow-600';
  return 'text-red-600';
}

function criteriaCount(criteria: Criterion[]): { met: number; total: number } {
  const met = criteria.filter((c) => c.met === true).length;
  return { met, total: criteria.length };
}

function CriterionStatusIcon({ met }: { met: boolean | null }) {
  if (met === true) {
    return <Check className="h-4 w-4 text-green-600 shrink-0" />;
  }
  if (met === false) {
    return <X className="h-4 w-4 text-red-600 shrink-0" />;
  }
  return <HelpCircle className="h-4 w-4 text-yellow-600 shrink-0" />;
}

function ProcessingView() {
  const [currentStep, setCurrentStep] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentStep((prev) => (prev < PROCESSING_STEPS.length ? prev + 1 : prev));
    }, 800);
    return () => clearInterval(interval);
  }, []);

  const allDone = currentStep >= PROCESSING_STEPS.length;

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-sm font-medium text-blue-700">
        <Sparkles className="h-4 w-4 animate-pulse" />
        {allDone ? 'Completing analysis...' : 'Analyzing encounter...'}
      </div>
      <ul className="space-y-2">
        {PROCESSING_STEPS.map((step, index) => {
          const isComplete = index < currentStep;
          const isCurrent = index === currentStep;

          return (
            <li
              key={step}
              className={`flex items-center gap-2 text-sm transition-all duration-500 ${
                isComplete
                  ? 'text-green-600'
                  : isCurrent
                    ? 'text-slate-800'
                    : 'text-slate-400'
              }`}
            >
              {isComplete ? (
                <Check className="h-3.5 w-3.5 text-green-600 shrink-0" />
              ) : isCurrent ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-500 shrink-0" />
              ) : (
                <span className="flex h-3.5 w-3.5 items-center justify-center shrink-0">
                  <span className="h-1.5 w-1.5 rounded-full bg-slate-300" />
                </span>
              )}
              {step}
            </li>
          );
        })}
      </ul>
      {/* Progress bar */}
      <div className="h-1 bg-slate-100 rounded-full overflow-hidden">
        <div
          className="h-full bg-blue-500 rounded-full transition-all duration-500 ease-out"
          style={{ width: `${Math.min((currentStep / PROCESSING_STEPS.length) * 100, 100)}%` }}
        />
      </div>
      {/* Finalizing indicator after all steps complete */}
      {allDone && (
        <div className="flex items-center gap-2 pt-1 text-sm text-blue-600 animate-pulse">
          <Loader2 className="h-3.5 w-3.5 animate-spin shrink-0" />
          Waiting for payer policy analysis...
        </div>
      )}
    </div>
  );
}

function ReviewingView({
  paRequest,
  onSubmit,
  onCriterionClick,
  onViewPdf,
}: {
  paRequest: PARequest;
  onSubmit: () => void;
  onCriterionClick?: (criterion: Criterion) => void;
  onViewPdf?: () => void;
}) {
  const { met, total } = criteriaCount(paRequest.criteria);

  return (
    <div className="space-y-4">
      {/* Confidence + criteria count */}
      <div className="flex items-center gap-4">
        <span className={`text-4xl font-bold ${confidenceColor(paRequest.confidence)}`}>
          {paRequest.confidence}%
        </span>
        <div className="text-sm text-slate-600">
          <div className="font-medium">{paRequest.procedureName}</div>
          <div>{met}/{total} met</div>
        </div>
      </div>

      {/* Criteria list */}
      <ul className="divide-y divide-slate-100">
        {paRequest.criteria.map((criterion) => (
          <li key={criterion.label}>
            <button
              type="button"
              className="flex w-full items-center gap-2 py-2 text-left text-sm hover:bg-slate-50 rounded px-1"
              onClick={() => onCriterionClick?.(criterion)}
            >
              <CriterionStatusIcon met={criterion.met} />
              <span className="flex-1">{criterion.label}</span>
              {criterion.reason && (
                <ChevronRight className="h-4 w-4 text-slate-400 shrink-0" />
              )}
            </button>
          </li>
        ))}
      </ul>

      {/* Clinical summary */}
      <details className="text-sm">
        <summary className="cursor-pointer font-medium text-slate-600 hover:text-slate-800">
          Clinical Summary
        </summary>
        <p className="mt-1 text-slate-500 whitespace-pre-wrap">{paRequest.clinicalSummary}</p>
      </details>

      {/* Action buttons */}
      <div className="flex gap-2">
        {onViewPdf && (
          <button
            type="button"
            className="flex flex-1 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
            onClick={onViewPdf}
          >
            <FileText className="h-4 w-4" />
            View PA Form
          </button>
        )}
        <button
          type="button"
          className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-blue-700 transition-colors"
          onClick={onSubmit}
        >
          <Send className="h-4 w-4" />
          Submit to {paRequest.payer}
        </button>
      </div>
    </div>
  );
}

function SubmittingView({ paRequest }: { paRequest: PARequest | null }) {
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm text-slate-600">
        <Loader2 className="h-4 w-4 animate-spin" />
        Submitting to {paRequest?.payer ?? 'payer'}...
      </div>
      <button
        type="button"
        disabled
        className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-400 px-4 py-2.5 text-sm font-medium text-white cursor-not-allowed"
      >
        <Loader2 className="h-4 w-4 animate-spin" />
        Submitting...
      </button>
    </div>
  );
}

function CompleteView({
  paRequest,
  onViewPdf,
}: {
  paRequest: PARequest | null;
  onViewPdf?: () => void;
}) {
  return (
    <div className="flex flex-col items-center gap-3 py-4 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-green-100">
        <CheckCircle2 className="h-6 w-6 text-green-600" />
      </div>
      <div>
        <div className="font-medium text-green-800">PA Submitted</div>
        <div className="text-sm text-slate-500">
          Submitted to {paRequest?.payer ?? 'payer'}
        </div>
        {paRequest?.submittedAt && (
          <div className="mt-1 text-xs text-slate-400">
            {new Date(paRequest.submittedAt).toLocaleString()}
          </div>
        )}
      </div>
      {onViewPdf && (
        <button
          type="button"
          className="mt-2 flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
          onClick={onViewPdf}
        >
          <FileText className="h-4 w-4" />
          View PA Form
        </button>
      )}
    </div>
  );
}

function ErrorView({ error }: { error: string }) {
  return (
    <div className="flex items-start gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
      <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
      <span>{error}</span>
    </div>
  );
}

export function PAResultsPanel({
  state,
  paRequest,
  error,
  onSubmit,
  onCriterionClick,
  onViewPdf,
}: PAResultsPanelProps) {
  const isProcessing = state === 'signing' || state === 'processing';

  return (
    <div className="rounded-lg border-l-4 border-blue-500 bg-white shadow-sm">
      {/* Header */}
      <div className="flex items-center gap-2 border-b border-slate-100 px-4 py-3">
        <ShieldCheck className="h-5 w-5 text-blue-600" />
        <span className="text-sm font-semibold text-slate-800">
          AuthScript — Prior Authorization
        </span>
      </div>

      {/* Body */}
      <div className="px-4 py-3">
        {state === 'idle' && (
          <p className="text-sm text-slate-400">
            Sign the encounter to begin PA analysis.
          </p>
        )}

        {isProcessing && <ProcessingView />}

        {state === 'reviewing' && paRequest && (
          <ReviewingView
            paRequest={paRequest}
            onSubmit={onSubmit}
            onCriterionClick={onCriterionClick}
            onViewPdf={onViewPdf}
          />
        )}

        {state === 'submitting' && <SubmittingView paRequest={paRequest} />}

        {state === 'complete' && <CompleteView paRequest={paRequest} onViewPdf={onViewPdf} />}

        {state === 'error' && error && <ErrorView error={error} />}
      </div>
    </div>
  );
}
