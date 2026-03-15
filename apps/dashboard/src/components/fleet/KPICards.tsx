import { useEffect, useRef, useState } from 'react';
import { cn } from '@/lib/utils';
import { animate } from 'motion';

export interface KPIStats {
  total: number;
  processing: number;
  ready: number;
  submitted: number;
  approved: number;
  denied: number;
}

interface KPICardsProps {
  stats: KPIStats;
  activeFilter: string | null;
  onFilter: (status: string) => void;
}

interface CardConfig {
  key: keyof KPIStats;
  label: string;
  borderColor: string;
  activeBg: string;
  activeBorder: string;
}

const CARDS: CardConfig[] = [
  {
    key: 'total',
    label: 'Total Cases',
    borderColor: 'border-t-teal-500',
    activeBg: 'bg-teal-50 dark:bg-teal-950/30',
    activeBorder: 'border-4 border-teal-500',
  },
  {
    key: 'processing',
    label: 'Processing',
    borderColor: 'border-t-blue-500',
    activeBg: 'bg-blue-50 dark:bg-blue-950/30',
    activeBorder: 'border-4 border-blue-500',
  },
  {
    key: 'ready',
    label: 'Ready',
    borderColor: 'border-t-purple-500',
    activeBg: 'bg-purple-50 dark:bg-purple-950/30',
    activeBorder: 'border-4 border-purple-500',
  },
  {
    key: 'submitted',
    label: 'Submitted',
    borderColor: 'border-t-amber-500',
    activeBg: 'bg-amber-50 dark:bg-amber-950/30',
    activeBorder: 'border-4 border-amber-500',
  },
  {
    key: 'approved',
    label: 'Approved',
    borderColor: 'border-t-green-500',
    activeBg: 'bg-green-50 dark:bg-green-950/30',
    activeBorder: 'border-4 border-green-500',
  },
  {
    key: 'denied',
    label: 'Denied',
    borderColor: 'border-t-red-500',
    activeBg: 'bg-red-50 dark:bg-red-950/30',
    activeBorder: 'border-4 border-red-500',
  },
];

/** Hook that animates a number from 0 to target using motion */
function useCountUp(target: number, duration = 0.8, delay = 0): number {
  const [value, setValue] = useState(0);
  const prevTarget = useRef(target);

  useEffect(() => {
    const timer = setTimeout(() => {
      const controls = animate(prevTarget.current === target ? 0 : prevTarget.current, target, {
        duration,
        ease: 'easeOut',
        onUpdate: (v) => setValue(Math.round(v)),
      });
      prevTarget.current = target;
      return () => controls.stop();
    }, delay);
    return () => clearTimeout(timer);
  }, [target, duration, delay]);

  return value;
}

function KPICardValue({ target, delay }: { target: number; delay: number }) {
  const displayValue = useCountUp(target, 0.8, delay);
  return <>{displayValue}</>;
}

export function KPICards({ stats, activeFilter, onFilter }: KPICardsProps) {
  return (
    <div className="grid grid-cols-6 gap-4">
      {CARDS.map((card, index) => {
        const isActive = activeFilter === card.key;
        return (
          <button
            key={card.key}
            data-testid={`kpi-card-${card.key}`}
            onClick={() => onFilter(card.key)}
            className={cn(
              'rounded-xl border border-border/50 bg-card p-4 text-left transition-all duration-200 hover:shadow-md cursor-pointer',
              `border-t-4 ${card.borderColor}`,
              isActive && `${card.activeBorder} ${card.activeBg}`,
              !isActive && 'border-t-4',
            )}
          >
            <span className="text-3xl font-bold tabular-nums">
              <KPICardValue target={stats[card.key]} delay={index * 100} />
            </span>
            <p className="mt-1 text-xs text-muted-foreground font-medium">
              {card.label}
            </p>
          </button>
        );
      })}
    </div>
  );
}
