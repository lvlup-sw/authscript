import { cn } from '@/lib/utils';
import { Shield, AlertTriangle, TrendingUp } from 'lucide-react';

interface ConfidenceMeterProps {
  score: number;
  showLabel?: boolean;
  variant?: 'default' | 'compact';
  className?: string;
}

const THRESHOLDS = { high: 0.8, medium: 0.5 };

function clampScore(score: number): number {
  if (typeof score !== 'number' || isNaN(score)) return 0;
  return Math.max(0, Math.min(1, score));
}

function getConfig(score: number) {
  if (score >= THRESHOLDS.high) {
    return {
      label: 'High Confidence',
      icon: Shield,
      gradient: 'from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)]',
      text: 'text-success',
      bg: 'bg-success/10',
    };
  }
  if (score >= THRESHOLDS.medium) {
    return {
      label: 'Medium Confidence',
      icon: TrendingUp,
      gradient: 'from-[hsl(38,92%,50%)] to-[hsl(25,95%,53%)]',
      text: 'text-warning',
      bg: 'bg-warning/10',
    };
  }
  return {
    label: 'Low Confidence',
    icon: AlertTriangle,
    gradient: 'from-[hsl(0,84%,60%)] to-[hsl(0,72%,50%)]',
    text: 'text-destructive',
    bg: 'bg-destructive/10',
  };
}

export function ConfidenceMeter({
  score,
  showLabel = false,
  variant = 'default',
  className,
}: ConfidenceMeterProps) {
  const clampedScore = clampScore(score);
  const percentage = Math.round(clampedScore * 100);
  const config = getConfig(clampedScore);
  const Icon = config.icon;

  return (
    <div className={cn('space-y-4', className)}>
      {/* Score Display */}
      <div className="flex items-center justify-between">
        {showLabel && (
          <div className={cn('inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium', config.bg, config.text)}>
            <Icon className="h-3.5 w-3.5" />
            {config.label}
          </div>
        )}
        <div className="flex items-baseline gap-1 ml-auto">
          <span className={cn('text-4xl font-bold tracking-tight', config.text)}>
            {percentage}
          </span>
          <span className="text-lg text-muted-foreground">%</span>
        </div>
      </div>

      {/* Progress Bar */}
      <div
        data-testid="confidence-meter"
        role="meter"
        aria-valuenow={percentage}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`AI confidence: ${percentage}%`}
        className={cn(
          'w-full rounded-full overflow-hidden bg-muted',
          variant === 'compact' ? 'h-2' : 'h-3'
        )}
      >
        <div
          data-testid="confidence-fill"
          className={cn(
            'h-full rounded-full bg-gradient-to-r transition-all duration-700 ease-out',
            config.gradient
          )}
          style={{ width: `${percentage}%` }}
        />
      </div>

      {/* Threshold Markers */}
      {variant === 'default' && (
        <div className="flex justify-between text-[10px] text-muted-foreground">
          <span>0%</span>
          <span className="text-warning">50%</span>
          <span className="text-success">80%</span>
          <span>100%</span>
        </div>
      )}
    </div>
  );
}

export default ConfidenceMeter;
