import { AnimatePresence } from 'motion/react';
import type { FleetPARequest } from '@/lib/fleetSeedData';
import { FleetCard } from './FleetCard';

interface FleetViewProps {
  requests: FleetPARequest[];
  filter: string | null;
  highlightedCaseId?: string;
  onSelectCase: (id: string) => void;
}

export function FleetView({
  requests,
  filter,
  highlightedCaseId,
  onSelectCase,
}: FleetViewProps) {
  const visible = filter
    ? requests.filter((r) => r.status === filter)
    : requests;

  return (
    <div className="grid grid-cols-6 gap-3">
      <AnimatePresence mode="popLayout">
        {visible.map((request, index) => (
          <FleetCard
            key={request.id}
            request={request}
            highlighted={request.id === highlightedCaseId}
            onSelect={onSelectCase}
            index={index}
          />
        ))}
      </AnimatePresence>
    </div>
  );
}
