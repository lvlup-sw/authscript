import {
  Check,
  X,
  HelpCircle,
  ChevronRight,
  ShieldAlert,
  Loader2,
  PenLine,
} from 'lucide-react';
import type { PreCheckCriterion } from '@/lib/demoData';
import type { DocState } from './useEhrDemoFlow';

export interface PAReadinessWidgetProps {
  state: 'checking' | 'ready';
  criteria: PreCheckCriterion[];
  order: { code: string; name: string };
  payer: string;
  policyId: string;
  onCriterionClick?: (criterion: PreCheckCriterion) => void;
  /** Called when user clicks the "Document" link on an indeterminate gap */
  onGapAction?: () => void;
  docState?: DocState;
}

function StatusIcon({ status }: { status: PreCheckCriterion['status'] }) {
  if (status === 'met') {
    return <Check className="h-4 w-4 text-emerald-600 shrink-0" />;
  }
  if (status === 'indeterminate') {
    return <HelpCircle className="h-4 w-4 text-amber-500 shrink-0" />;
  }
  return <X className="h-4 w-4 text-red-600 shrink-0" />;
}

function ProgressDots({ met, total, allMet }: { met: number; total: number; allMet: boolean }) {
  return (
    <span className="inline-flex items-center gap-0.5">
      {Array.from({ length: total }, (_, i) => (
        <span
          key={i}
          className={`inline-block h-1.5 w-1.5 rounded-full transition-colors duration-500 ${
            i < met ? (allMet ? 'bg-emerald-500' : 'bg-amber-500') : 'bg-slate-200'
          }`}
        />
      ))}
    </span>
  );
}

export function PAReadinessWidget({
  state,
  criteria,
  order,
  payer,
  policyId,
  onCriterionClick,
  onGapAction,
  docState = 'idle',
}: PAReadinessWidgetProps) {
  const metCount = criteria.filter((c) => c.status === 'met').length;
  const total = criteria.length;
  const allMet = metCount === total;

  return (
    <div className={`rounded-lg border-l-4 bg-white shadow-sm transition-colors duration-700 ${allMet ? 'border-emerald-500' : 'border-amber-500'}`}>
      {/* Header */}
      <div className="flex items-center gap-2 border-b border-slate-100 px-4 py-3">
        <ShieldAlert className={`h-5 w-5 transition-colors duration-700 ${allMet ? 'text-emerald-600' : 'text-amber-600'}`} />
        <span className="text-sm font-semibold text-slate-800">
          AuthScript &mdash; Policy Pre-Check
        </span>
        <span className="ml-auto font-mono text-xs tracking-wide uppercase text-slate-400">
          {policyId}
        </span>
      </div>

      {/* Body */}
      <div className="px-4 py-3">
        {state === 'checking' && (
          <div className="flex items-center gap-2 text-sm text-slate-600">
            <Loader2 className="h-4 w-4 animate-spin text-amber-500 shrink-0" />
            <span className="animate-pulse">
              Analyzing patient chart against payer policy...
            </span>
          </div>
        )}

        {state === 'ready' && (
          <div className="space-y-3">
            {/* Summary row */}
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-slate-600">
              <span className="font-medium text-slate-800">
                {order.name}
              </span>
              <span className="text-slate-400">|</span>
              <span>{payer}</span>
              <span className="text-slate-400">|</span>
              <span className="inline-flex items-center gap-1.5">
                <ProgressDots met={metCount} total={total} allMet={allMet} />
                <span>
                  {metCount}/{total} criteria documented
                </span>
              </span>
            </div>

            {/* Criteria list */}
            <ul className="divide-y divide-slate-100">
              {criteria.map((criterion) => {
                const isClickable = !!onCriterionClick;
                const Row = isClickable ? 'button' : 'div';

                return (
                  <li key={criterion.label}>
                    <Row
                      type={isClickable ? 'button' : undefined}
                      className={`flex w-full items-start gap-2 py-2.5 px-1 text-left transition-colors duration-500 ${
                        isClickable ? 'hover:bg-slate-50 rounded cursor-pointer' : ''
                      }`}
                      onClick={
                        isClickable
                          ? () => onCriterionClick(criterion)
                          : undefined
                      }
                    >
                      <span className="mt-0.5">
                        <StatusIcon status={criterion.status} />
                      </span>
                      <span className="flex-1 min-w-0">
                        <span className="text-sm font-medium text-slate-800">
                          {criterion.label}
                        </span>
                        {criterion.status === 'met' && criterion.evidence && (
                          <span className="block mt-0.5">
                            <span className="text-sm italic text-slate-500">
                              {criterion.evidence}
                            </span>
                            {criterion.source && (
                              <span className="inline-flex items-center rounded px-1.5 py-0.5 bg-slate-100 font-mono text-[10px] uppercase tracking-widest text-slate-500 ml-1.5">
                                {criterion.source}
                              </span>
                            )}
                          </span>
                        )}
                        {criterion.status === 'indeterminate' && criterion.gap && (
                          <span className="block mt-0.5 text-sm text-amber-600">
                            {criterion.gap}
                            {/* Inline "Document" action link */}
                            {onGapAction && docState === 'idle' && (
                              <button
                                type="button"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  onGapAction();
                                }}
                                className="ml-2 inline-flex items-center gap-1 text-xs font-medium text-blue-600 hover:text-blue-800 underline decoration-dotted underline-offset-2"
                              >
                                <PenLine className="h-3 w-3" />
                                Document
                              </button>
                            )}
                            {docState !== 'idle' && docState !== 'saved' && (
                              <span className="ml-2 inline-flex items-center gap-1 text-xs text-slate-400">
                                <Loader2 className="h-3 w-3 animate-spin" />
                                Editing...
                              </span>
                            )}
                          </span>
                        )}
                        {criterion.status === 'not-met' && criterion.gap && (
                          <span className="block mt-0.5 text-sm text-red-600">
                            {criterion.gap}
                          </span>
                        )}
                      </span>
                      {isClickable && (
                        <ChevronRight className="h-4 w-4 text-slate-400 shrink-0 mt-0.5" />
                      )}
                    </Row>
                  </li>
                );
              })}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}
