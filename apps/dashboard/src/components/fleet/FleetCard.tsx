import { cn } from '@/lib/utils';
import type { FleetPARequest, FleetStatus } from '@/lib/fleetSeedData';

interface FleetCardProps {
  request: FleetPARequest;
  highlighted?: boolean;
  onSelect: (id: string) => void;
}

const STATUS_COLORS: Record<FleetStatus, string> = {
  processing: 'bg-blue-500',
  ready: 'bg-purple-500',
  submitted: 'bg-amber-500',
  waiting_for_insurance: 'bg-sky-500',
  approved: 'bg-green-500',
  denied: 'bg-red-500',
};

/** Statuses where confidence has been computed */
const ANALYZED_STATUSES: FleetStatus[] = [
  'ready',
  'submitted',
  'waiting_for_insurance',
  'approved',
  'denied',
];

function getInitials(name: string): string {
  return name
    .split(' ')
    .map((part) => part[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

export function FleetCard({ request, highlighted = false, onSelect }: FleetCardProps) {
  const initials = getInitials(request.patient.name);
  const showConfidence = ANALYZED_STATUSES.includes(request.status);

  return (
    <div
      data-testid={`fleet-card-${request.id}`}
      onClick={() => onSelect(request.id)}
      className={cn(
        'rounded-lg border border-border/50 bg-card p-2.5 cursor-pointer transition-all duration-200 hover:shadow-md',
        highlighted && 'ring-2 ring-teal-400/50 shadow-lg shadow-teal-500/20',
      )}
    >
      <div className="flex items-center gap-2">
        {/* Avatar with initials */}
        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold text-muted-foreground">
          {initials}
        </div>

        {/* Status dot */}
        <span
          data-testid="status-dot"
          className={cn(
            'inline-block h-2 w-2 shrink-0 rounded-full',
            STATUS_COLORS[request.status],
          )}
        />

        {/* CPT code */}
        <span className="text-xs font-mono font-medium text-foreground">
          {request.procedureCode}
        </span>
      </div>

      <div className="mt-1.5 flex items-center justify-between gap-1">
        {/* Payer badge */}
        <span className="inline-block rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground truncate">
          {request.payer}
        </span>

        {/* Confidence */}
        {showConfidence && (
          <span className="text-[10px] font-semibold text-foreground tabular-nums">
            {request.confidence}%
          </span>
        )}
      </div>
    </div>
  );
}
