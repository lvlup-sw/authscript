import { cn } from '@/lib/utils';

interface ConfidenceMeterProps {
  score: number;
  showLabel?: boolean;
  variant?: 'default' | 'compact';
  className?: string;
}

/**
 * Confidence thresholds for color coding
 */
const CONFIDENCE_THRESHOLDS = {
  high: 0.8,
  medium: 0.5,
} as const;

/**
 * Get color class based on confidence score
 */
function getConfidenceColor(score: number): string {
  if (score >= CONFIDENCE_THRESHOLDS.high) {
    return 'bg-green-500';
  }
  if (score >= CONFIDENCE_THRESHOLDS.medium) {
    return 'bg-yellow-500';
  }
  return 'bg-red-500';
}

/**
 * Get confidence label based on score
 */
function getConfidenceLabel(score: number): string {
  if (score >= CONFIDENCE_THRESHOLDS.high) {
    return 'High Confidence';
  }
  if (score >= CONFIDENCE_THRESHOLDS.medium) {
    return 'Medium Confidence';
  }
  return 'Low Confidence';
}

/**
 * Clamp value between 0 and 1
 */
function clampScore(score: number): number {
  if (typeof score !== 'number' || isNaN(score)) {
    return 0;
  }
  return Math.max(0, Math.min(1, score));
}

/**
 * Confidence Meter Component
 * Visual indicator of AI confidence score with color coding
 */
export function ConfidenceMeter({
  score,
  showLabel = false,
  variant = 'default',
  className,
}: ConfidenceMeterProps) {
  const clampedScore = clampScore(score);
  const percentage = Math.round(clampedScore * 100);
  const colorClass = getConfidenceColor(clampedScore);
  const label = getConfidenceLabel(clampedScore);

  return (
    <div className={cn('space-y-1', className)}>
      {/* Label and percentage */}
      <div className="flex items-center justify-between text-sm">
        {showLabel && (
          <span className="text-muted-foreground">{label}</span>
        )}
        <span className="font-medium">{percentage}%</span>
      </div>

      {/* Meter bar */}
      <div
        role="meter"
        aria-valuenow={percentage}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`AI confidence: ${percentage}%`}
        data-testid="confidence-container"
        className={cn(
          'w-full bg-muted rounded-full overflow-hidden',
          variant === 'compact' ? 'h-2' : 'h-4'
        )}
      >
        <div
          data-testid="confidence-fill"
          className={cn(
            'h-full transition-all duration-300 rounded-full',
            colorClass
          )}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}

export default ConfidenceMeter;
