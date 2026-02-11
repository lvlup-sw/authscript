/**
 * Overlay shown during PA submission, mimicking real processing.
 * Uses same visual style as NewPAModal processing state.
 * Phases: "Locating the correct submission method" → "Found submission <name>"
 */

import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { Search, Check, CheckCircle2 } from 'lucide-react';
import { Sparkles } from 'lucide-react';
import { LoadingSpinner } from './LoadingSpinner';
import { cn } from '@/lib/utils';

const PHASE_DURATIONS = {
  locating: 2800,
  found: 2200,
} as const;

const SUBMISSION_STEPS = [
  { icon: Search, label: 'Locating the correct submission method' },
  { icon: CheckCircle2, label: 'Found submission', isDynamic: true },
] as const;

export type SubmissionPhase = 'locating' | 'found';

export interface SubmissionProgressOverlayProps {
  /** Submission method name to display when found (e.g. "BCBS ePA Portal") */
  submissionName: string;
  /** Callback when animation completes */
  onComplete?: () => void;
}

export function SubmissionProgressOverlay({
  submissionName,
  onComplete,
}: SubmissionProgressOverlayProps) {
  const [phase, setPhase] = useState<SubmissionPhase>('locating');
  const processingStep = phase === 'locating' ? 0 : 1;

  useEffect(() => {
    const t1 = setTimeout(() => {
      setPhase('found');
    }, PHASE_DURATIONS.locating);

    const totalDuration = PHASE_DURATIONS.locating + PHASE_DURATIONS.found;
    const t2 = setTimeout(() => {
      onComplete?.();
    }, totalDuration);

    return () => {
      clearTimeout(t1);
      clearTimeout(t2);
    };
  }, [onComplete]);

  return createPortal(
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 backdrop-blur-sm animate-fade-in"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4 border border-gray-200 overflow-hidden">
        <div className="flex flex-col items-center justify-center py-8 px-6">
          {/* Animated Logo - matches NewPAModal */}
          <div className="relative mb-6">
            <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-teal to-teal/80 flex items-center justify-center shadow-lg">
              <Sparkles className="w-10 h-10 text-white animate-pulse" />
            </div>
            <div className="absolute -bottom-2 -right-2 w-8 h-8 bg-white rounded-full shadow-md flex items-center justify-center">
              <LoadingSpinner size="sm" />
            </div>
          </div>

          <h3 className="text-xl font-bold text-gray-900 mb-2">Submitting PA Request</h3>
          <p className="text-gray-500 text-center max-w-sm mb-6">
            {phase === 'locating'
              ? 'Checking payer requirements and available channels...'
              : 'Submitting your PA request...'}
          </p>

          {/* Processing Steps - matches NewPAModal step list */}
          <div className="w-full max-w-sm space-y-3">
            {SUBMISSION_STEPS.map((item, index) => {
              const Icon = item.icon;
              const isComplete = index < processingStep;
              const isCurrent = index === processingStep;
              const isPending = index > processingStep;

              return (
                <div
                  key={index}
                  className={cn(
                    'flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-500',
                    isComplete && 'bg-teal/5',
                    isCurrent && 'bg-teal/10 border border-teal/20',
                    isPending && 'opacity-40'
                  )}
                >
                  <div
                    className={cn(
                      'w-8 h-8 rounded-lg flex items-center justify-center transition-all duration-500',
                      isComplete && 'bg-teal text-white',
                      isCurrent && 'bg-teal/20 text-teal',
                      isPending && 'bg-gray-200 text-gray-400'
                    )}
                  >
                    {isComplete ? (
                      <Check className="w-4 h-4" />
                    ) : (
                      <Icon className="w-4 h-4" />
                    )}
                  </div>
                  <span
                    className={cn(
                      'text-sm font-medium flex-1 transition-all duration-500',
                      isComplete && 'text-teal',
                      isCurrent && 'text-gray-900',
                      isPending && 'text-gray-400'
                    )}
                  >
                    {'isDynamic' in item && item.isDynamic && index === 1
                      ? `Found submission ${submissionName}`
                      : item.label}
                  </span>
                  {isCurrent && <LoadingSpinner size="sm" />}
                  {isComplete && <Check className="w-4 h-4 text-teal" />}
                </div>
              );
            })}
          </div>

          {/* Progress Bar - matches NewPAModal */}
          <div className="w-full max-w-sm mt-6">
            <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
              <div
                className="h-full bg-teal rounded-full transition-all duration-500 ease-out"
                style={{
                  width: `${((processingStep + 1) / SUBMISSION_STEPS.length) * 100}%`,
                }}
              />
            </div>
          </div>
        </div>
      </div>
    </div>,
    document.body
  );
}

/** Maps payer name to a submission method name (for demo purposes) */
export function getSubmissionNameForPayer(payer: string): string {
  const mapping: Record<string, string> = {
    'Blue Cross Blue Shield': 'BCBS ePA Portal',
    'Aetna': 'Aetna ePA',
    'United Healthcare': 'United Healthcare ePA',
    'Cigna': 'Cigna PriorAuthNow',
    'Humana': 'Humana ePA',
  };
  return mapping[payer] ?? `${payer} Electronic Portal`;
}
