import { cn } from '@/lib/utils';

interface CasePipelineProps {
  stageCounts: Record<string, number>;
  activeStage: string | null;
  onFilter: (stage: string) => void;
}

interface StageConfig {
  key: string;
  label: string;
}

const STAGES: StageConfig[] = [
  { key: 'order_signed', label: 'Order Signed' },
  { key: 'pa_detected', label: 'PA Detected' },
  { key: 'processing', label: 'Processing' },
  { key: 'ready', label: 'Ready' },
  { key: 'submitted', label: 'Submitted' },
  { key: 'payer_response', label: 'Payer Response' },
];

export function CasePipeline({ stageCounts, activeStage, onFilter }: CasePipelineProps) {
  return (
    <div
      className="relative flex items-center justify-between px-4 py-6"
      aria-label="Case progress pipeline"
    >
      {/* SVG connecting lines */}
      <svg
        className="absolute inset-0 w-full h-full pointer-events-none"
        preserveAspectRatio="none"
        aria-hidden="true"
      >
        <defs>
          <style>{`
            @keyframes flowDot {
              0% { stroke-dashoffset: 24; }
              100% { stroke-dashoffset: 0; }
            }
          `}</style>
        </defs>
        {STAGES.slice(0, -1).map((_, i) => {
          const x1 = `${((i + 0.5) / STAGES.length) * 100 + 3}%`;
          const x2 = `${((i + 1.5) / STAGES.length) * 100 - 3}%`;
          return (
            <g key={i}>
              <line
                x1={x1}
                y1="40%"
                x2={x2}
                y2="40%"
                stroke="currentColor"
                strokeWidth="2"
                className="text-border"
              />
              <line
                x1={x1}
                y1="40%"
                x2={x2}
                y2="40%"
                stroke="currentColor"
                strokeWidth="2"
                strokeDasharray="4 4"
                className="text-teal-400/40"
                style={{ animation: 'flowDot 1.5s linear infinite' }}
              />
            </g>
          );
        })}
      </svg>

      {STAGES.map((stage) => {
        const count = stageCounts[stage.key] ?? 0;
        const isActive = activeStage === stage.key;

        return (
          <button
            key={stage.key}
            data-testid={`pipeline-stage-${stage.key}`}
            onClick={() => onFilter(stage.key)}
            className={cn(
              'relative z-10 flex flex-col items-center gap-2 cursor-pointer group',
              isActive && 'ring-2 ring-teal-400/30 rounded-xl bg-teal-50/50 dark:bg-teal-950/20 px-3 py-1',
            )}
          >
            {/* Circle with count */}
            <div
              data-testid={`stage-count-${stage.key}`}
              className={cn(
                'flex h-12 w-12 items-center justify-center rounded-full text-sm font-bold transition-all duration-200',
                isActive
                  ? 'bg-teal-500 text-white shadow-lg shadow-teal-500/30 ring-4 ring-teal-400/20'
                  : 'bg-card border-2 border-border text-foreground group-hover:border-teal-400/50 group-hover:shadow-md',
              )}
            >
              {count}
            </div>

            {/* Label */}
            <span
              className={cn(
                'text-xs font-medium whitespace-nowrap transition-colors',
                isActive ? 'text-teal-600 dark:text-teal-400' : 'text-muted-foreground',
              )}
            >
              {stage.label}
            </span>
          </button>
        );
      })}
    </div>
  );
}
