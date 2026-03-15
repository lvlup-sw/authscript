import { cn } from '@/lib/utils';
import type { ReactNode } from 'react';

interface NodeCardProps {
  children: ReactNode;
  className?: string;
  borderColor?: string;
}

/**
 * Shared card wrapper for React Flow custom nodes.
 * Provides consistent border, padding, rounded corners, and shadow.
 */
export function NodeCard({ children, className, borderColor }: NodeCardProps) {
  return (
    <div
      className={cn(
        'bg-white rounded-xl border-2 px-4 py-3 shadow-md min-w-[180px]',
        borderColor ?? 'border-slate-200',
        className,
      )}
    >
      {children}
    </div>
  );
}
