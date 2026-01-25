import React from 'react';
import { cn } from '@/lib/utils';

interface LoadingSpinnerProps {
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

interface LoadingStateProps {
  title?: string;
  subtitle?: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
  showIcon?: boolean;
}

interface LoadingOverlayProps {
  isLoading: boolean;
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  blur?: boolean;
  className?: string;
}

interface LoadingButtonProps {
  isLoading: boolean;
  children: React.ReactNode;
  loadingText?: string;
  className?: string;
  disabled?: boolean;
  size?: 'xs' | 'sm' | 'md' | 'lg';
}

interface SkeletonProps {
  className?: string;
  rows?: number;
  variant?: 'text' | 'circular' | 'rectangular' | 'card';
}

const sizeClasses = {
  xs: 'w-3 h-3',
  sm: 'w-4 h-4',
  md: 'w-6 h-6',
  lg: 'w-8 h-8',
  xl: 'w-12 h-12',
};

/**
 * Basic loading spinner component with brand-consistent styling
 */
export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  className
}) => {
  return (
    <div
      className={cn(
        'animate-spin rounded-full border-2 border-muted border-t-accent',
        sizeClasses[size],
        className
      )}
    />
  );
};

/**
 * Loading state with optional title and subtitle
 */
export const LoadingState: React.FC<LoadingStateProps> = ({
  title = 'Loading...',
  subtitle,
  size = 'lg',
  className,
  showIcon = true
}) => {
  return (
    <div className={cn('flex flex-col items-center justify-center py-8 px-4 text-center', className)}>
      {showIcon && (
        <LoadingSpinner size={size} className="mb-4" />
      )}
      <h3 className="text-lg font-medium text-foreground mb-2">{title}</h3>
      {subtitle && (
        <p className="text-sm text-muted-foreground max-w-sm">{subtitle}</p>
      )}
    </div>
  );
};

/**
 * Loading overlay that covers content while loading
 */
export const LoadingOverlay: React.FC<LoadingOverlayProps> = ({
  isLoading,
  title,
  subtitle,
  children,
  blur = true,
  className
}) => {
  return (
    <div className={cn('relative', className)}>
      {children}
      {isLoading && (
        <div className={cn(
          'absolute inset-0 bg-background/80 z-10 flex items-center justify-center transition-opacity duration-200',
          blur && 'backdrop-blur-sm'
        )}>
          <LoadingState
            title={title}
            subtitle={subtitle}
            size="lg"
          />
        </div>
      )}
    </div>
  );
};

/**
 * Button with loading state
 */
export const LoadingButton: React.FC<LoadingButtonProps> = ({
  isLoading,
  children,
  loadingText,
  className,
  disabled,
  size = 'md'
}) => {
  const spinnerSize = size === 'xs' ? 'xs' : size === 'sm' ? 'sm' : 'sm';

  return (
    <button
      className={cn(
        'inline-flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed transition-opacity duration-200',
        className
      )}
      disabled={disabled || isLoading}
    >
      {isLoading && <LoadingSpinner size={spinnerSize} />}
      <span>{isLoading && loadingText ? loadingText : children}</span>
    </button>
  );
};

/**
 * Skeleton loading placeholders
 */
export const Skeleton: React.FC<SkeletonProps> = ({
  className,
  rows = 1,
  variant = 'text'
}) => {
  if (variant === 'card') {
    return (
      <div className={cn('animate-pulse', className)}>
        <div className="rounded-lg bg-muted h-48 w-full mb-4"></div>
        <div className="space-y-3">
          <div className="h-4 bg-muted rounded w-3/4"></div>
          <div className="h-4 bg-muted rounded w-1/2"></div>
        </div>
      </div>
    );
  }

  if (variant === 'circular') {
    return (
      <div className={cn('rounded-full bg-muted animate-pulse w-12 h-12', className)} />
    );
  }

  if (variant === 'rectangular') {
    return (
      <div className={cn('bg-muted animate-pulse h-32 w-full rounded', className)} />
    );
  }

  // Text variant (default)
  return (
    <div className={cn('animate-pulse space-y-2', className)}>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex space-x-4">
          <div className="h-4 bg-muted rounded flex-1"></div>
          {i === rows - 1 && rows > 1 && (
            <div className="h-4 bg-muted rounded w-16"></div>
          )}
        </div>
      ))}
    </div>
  );
};

/**
 * Page-level loading component
 */
export const PageLoading: React.FC<{
  title?: string;
  subtitle?: string;
  fullScreen?: boolean;
}> = ({
  title = 'Loading...',
  subtitle,
  fullScreen = false
}) => {
  return (
    <div className={cn(
      'flex items-center justify-center',
      fullScreen ? 'min-h-screen' : 'min-h-96',
      'bg-muted/50'
    )}>
      <LoadingState
        title={title}
        subtitle={subtitle}
        size="xl"
        className="bg-card rounded-lg shadow-sm border border-border p-8 max-w-sm mx-4"
      />
    </div>
  );
};

/**
 * Inline loading component for smaller spaces
 */
export const InlineLoading: React.FC<{
  text?: string;
  size?: 'xs' | 'sm' | 'md';
}> = ({
  text = 'Loading...',
  size = 'sm'
}) => {
  return (
    <div className="flex items-center gap-2 text-muted-foreground">
      <LoadingSpinner size={size} />
      <span className="text-sm">{text}</span>
    </div>
  );
};

/**
 * Table loading skeleton
 */
export const TableSkeleton: React.FC<{
  rows?: number;
  columns?: number;
}> = ({ rows = 5, columns = 4 }) => {
  return (
    <div className="animate-pulse">
      {/* Header */}
      <div className="flex space-x-4 mb-4">
        {Array.from({ length: columns }).map((_, i) => (
          <div key={i} className="h-4 bg-muted-foreground/20 rounded flex-1"></div>
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex space-x-4 mb-3">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <div key={colIndex} className="h-4 bg-muted rounded flex-1"></div>
          ))}
        </div>
      ))}
    </div>
  );
};

/**
 * List loading skeleton
 */
export const ListSkeleton: React.FC<{
  items?: number;
  showAvatar?: boolean;
}> = ({ items = 5, showAvatar = false }) => {
  return (
    <div className="space-y-4">
      {Array.from({ length: items }).map((_, i) => (
        <div key={i} className="flex items-center space-x-4 animate-pulse">
          {showAvatar && (
            <div className="rounded-full bg-muted h-10 w-10"></div>
          )}
          <div className="flex-1 space-y-2">
            <div className="h-4 bg-muted rounded w-3/4"></div>
            <div className="h-3 bg-muted rounded w-1/2"></div>
          </div>
        </div>
      ))}
    </div>
  );
};
