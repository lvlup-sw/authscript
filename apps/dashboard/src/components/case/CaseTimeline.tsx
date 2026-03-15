import { cn } from '@/lib/utils';

export interface TimelinePhase {
  name: string;
  status: 'completed' | 'active' | 'pending';
  timestamp?: string;
  duration?: string;
}

interface CaseTimelineProps {
  phases: TimelinePhase[];
}

/**
 * Vertical timeline showing PA request lifecycle phases.
 * Completed phases show a green checkmark, active phase pulses,
 * and pending phases are muted with dashed connectors.
 */
export function CaseTimeline({ phases }: CaseTimelineProps) {
  return (
    <div data-testid="case-timeline" className="space-y-0">
      {phases.map((phase, i) => {
        const isLast = i === phases.length - 1;

        return (
          <div key={phase.name} className="flex gap-3">
            {/* Timeline column: icon + connector line */}
            <div className="flex flex-col items-center">
              {/* Status icon */}
              {phase.status === 'completed' && (
                <div
                  data-testid="phase-check"
                  aria-label="completed"
                  className="w-6 h-6 rounded-full bg-green-500 flex items-center justify-center flex-shrink-0"
                >
                  <span className="text-white text-xs font-bold">{'\u2713'}</span>
                </div>
              )}
              {phase.status === 'active' && (
                <div
                  data-testid="phase-active"
                  aria-label="active"
                  className="relative w-6 h-6 flex items-center justify-center flex-shrink-0"
                >
                  <span className="absolute inline-flex h-full w-full rounded-full bg-teal opacity-30 animate-ping" />
                  <span className="relative inline-flex w-4 h-4 rounded-full bg-teal" />
                </div>
              )}
              {phase.status === 'pending' && (
                <div
                  data-testid="phase-pending"
                  aria-label="pending"
                  className="w-6 h-6 rounded-full border-2 border-dashed border-gray-300 flex items-center justify-center flex-shrink-0"
                >
                  <span className="w-2 h-2 rounded-full bg-gray-300" />
                </div>
              )}

              {/* Connector line */}
              {!isLast && (
                <div
                  className={cn(
                    'w-0.5 flex-1 min-h-[24px]',
                    phase.status === 'completed'
                      ? 'bg-green-400'
                      : phase.status === 'active'
                        ? 'bg-teal/40'
                        : 'border-l-2 border-dashed border-gray-200',
                  )}
                />
              )}
            </div>

            {/* Content column */}
            <div className={cn('pb-4 flex-1 min-w-0', isLast && 'pb-0')}>
              <p
                className={cn(
                  'text-sm leading-tight',
                  phase.status === 'completed' && 'text-gray-600',
                  phase.status === 'active' && 'font-bold text-teal',
                  phase.status === 'pending' && 'text-gray-400',
                )}
              >
                {phase.name}
              </p>
              <div className="flex items-center gap-2 mt-0.5">
                {phase.timestamp && (
                  <span className="text-[11px] text-gray-400">
                    {phase.timestamp}
                  </span>
                )}
                {phase.duration && (
                  <span className="text-[11px] text-gray-400 font-mono">
                    {phase.duration}
                  </span>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
