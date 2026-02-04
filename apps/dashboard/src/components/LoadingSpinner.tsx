import { cn } from '@/lib/utils';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

export function LoadingSpinner({ size = 'md', className }: LoadingSpinnerProps) {
  const sizeClasses = {
    sm: 'w-4 h-4 border-2',
    md: 'w-8 h-8 border-3',
    lg: 'w-12 h-12 border-4',
  };

  return (
    <div
      className={cn(
        'rounded-full border-teal border-t-transparent animate-spin',
        sizeClasses[size],
        className
      )}
    />
  );
}

// Full page loader
export function PageLoader() {
  return (
    <div className="fixed inset-0 bg-white/80 backdrop-blur-sm z-50 flex items-center justify-center">
      <div className="flex flex-col items-center gap-4">
        <div className="relative">
          <div className="w-16 h-16 rounded-2xl bg-teal flex items-center justify-center animate-pulse">
            <span className="text-white font-bold text-xl">AS</span>
          </div>
          <div className="absolute -bottom-1 -right-1 w-6 h-6 bg-white rounded-full flex items-center justify-center shadow-md">
            <LoadingSpinner size="sm" />
          </div>
        </div>
        <p className="text-sm text-gray-500 animate-pulse">Loading...</p>
      </div>
    </div>
  );
}

// Skeleton components for loading states
export function Skeleton({ className }: { className?: string }) {
  return (
    <div className={cn('animate-pulse bg-gray-200 rounded', className)} />
  );
}

export function SkeletonCard() {
  return (
    <div className="bg-white rounded-2xl border border-gray-200 p-5 space-y-4">
      <div className="flex items-center gap-3">
        <Skeleton className="w-10 h-10 rounded-xl" />
        <div className="space-y-2 flex-1">
          <Skeleton className="h-4 w-1/3" />
          <Skeleton className="h-3 w-1/2" />
        </div>
      </div>
      <Skeleton className="h-20 w-full rounded-lg" />
    </div>
  );
}

export function SkeletonRow() {
  return (
    <div className="flex items-center gap-4 p-4 rounded-xl border border-gray-100">
      <Skeleton className="w-11 h-11 rounded-xl" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-4 w-1/4" />
        <Skeleton className="h-3 w-1/3" />
      </div>
      <Skeleton className="h-8 w-20 rounded-lg" />
    </div>
  );
}

export function SkeletonStats() {
  return (
    <div className="grid grid-cols-4 gap-4">
      {[1, 2, 3, 4].map((i) => (
        <div key={i} className="bg-white rounded-2xl border border-gray-200 p-5 space-y-3">
          <Skeleton className="w-10 h-10 rounded-xl mx-auto" />
          <Skeleton className="h-8 w-12 mx-auto" />
          <Skeleton className="h-4 w-24 mx-auto" />
        </div>
      ))}
    </div>
  );
}

// Processing indicator with steps
interface ProcessingStepProps {
  steps: string[];
  currentStep: number;
  title?: string;
}

export function ProcessingSteps({ steps, currentStep, title = 'Processing' }: ProcessingStepProps) {
  return (
    <div className="flex flex-col items-center py-8">
      <div className="w-16 h-16 rounded-2xl bg-teal flex items-center justify-center mb-4 shadow-lg relative">
        <span className="text-white font-bold text-xl">AS</span>
        <div className="absolute -bottom-1 -right-1">
          <LoadingSpinner size="sm" />
        </div>
      </div>
      
      <h3 className="text-lg font-semibold text-gray-900 mb-2">{title}</h3>
      
      <div className="space-y-2 mt-4">
        {steps.map((step, index) => (
          <div 
            key={step}
            className={cn(
              'flex items-center gap-3 px-4 py-2 rounded-lg transition-all duration-300',
              index < currentStep && 'text-teal',
              index === currentStep && 'text-gray-900 bg-gray-100',
              index > currentStep && 'text-gray-400'
            )}
          >
            <div className={cn(
              'w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold transition-all',
              index < currentStep && 'bg-teal text-white',
              index === currentStep && 'bg-teal/20 text-teal border-2 border-teal',
              index > currentStep && 'bg-gray-200 text-gray-400'
            )}>
              {index < currentStep ? '✓' : index + 1}
            </div>
            <span className="text-sm font-medium">{step}</span>
            {index === currentStep && (
              <LoadingSpinner size="sm" className="ml-2" />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

// Button with loading state
interface LoadingButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  loading?: boolean;
  loadingText?: string;
  children: React.ReactNode;
}

export function LoadingButton({ 
  loading, 
  loadingText, 
  children, 
  className,
  disabled,
  ...props 
}: LoadingButtonProps) {
  return (
    <button
      className={cn(
        'relative flex items-center justify-center gap-2 transition-all',
        loading && 'cursor-not-allowed',
        className
      )}
      disabled={disabled || loading}
      {...props}
    >
      {loading && (
        <LoadingSpinner size="sm" className="absolute left-1/2 -translate-x-1/2" />
      )}
      <span className={cn(loading && 'opacity-0')}>
        {children}
      </span>
      {loading && loadingText && (
        <span className="absolute left-1/2 -translate-x-1/2 whitespace-nowrap pl-6">
          {loadingText}
        </span>
      )}
    </button>
  );
}
