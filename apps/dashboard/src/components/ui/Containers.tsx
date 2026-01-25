import { cn } from '@/lib/utils';
import { ReactNode } from 'react';

interface ContainerProps {
  children: ReactNode;
  className?: string;
}

/**
 * Basic container with padding and background
 * Use for content sections that don't need scrolling
 */
export function Container({ children, className }: ContainerProps) {
  return (
    <div
      className={cn(
        'm-6 p-6 max-w-full box-border bg-card rounded-lg',
        className
      )}
    >
      {children}
    </div>
  );
}

/**
 * Minimal scroll wrapper with no additional styling
 * Use when you need just scrolling behavior
 */
export function HeadlessScroll({ children, className }: ContainerProps) {
  return (
    <div
      className={cn(
        'overflow-y-auto min-h-0 max-h-full',
        className
      )}
    >
      {children}
    </div>
  );
}

/**
 * Full page scrollable container
 * Use as the main wrapper for page content (only one per page)
 */
export function ScrollPage({ children, className }: ContainerProps) {
  return (
    <div
      className={cn(
        'm-6 bg-card rounded-lg overflow-y-auto',
        className
      )}
    >
      {children}
    </div>
  );
}

/**
 * Combines Container styling with scroll behavior
 * Use for scrollable sections within a page
 */
export function ScrollContainer({ children, className }: ContainerProps) {
  return (
    <div
      className={cn(
        'm-6 p-6 max-w-full box-border bg-card rounded-lg overflow-y-auto',
        className
      )}
    >
      {children}
    </div>
  );
}

/**
 * Container that fills available viewport height
 * Use for layouts that need to fill the screen
 */
export function FullScreenContainer({ children, className }: ContainerProps) {
  return (
    <div
      className={cn(
        'h-[calc(100vh-48px)] max-h-full overflow-hidden',
        className
      )}
    >
      {children}
    </div>
  );
}
