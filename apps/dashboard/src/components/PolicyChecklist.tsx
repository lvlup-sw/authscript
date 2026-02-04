import { cn } from '@/lib/utils';
import { CheckCircle2, XCircle, HelpCircle, Circle, Shield } from 'lucide-react';

interface PolicyCriterion {
  id: string;
  description: string;
  status: 'met' | 'not_met' | 'unclear' | 'pending';
}

interface PolicyChecklistProps {
  transactionId: string;
  className?: string;
}

const statusConfig = {
  met: {
    icon: CheckCircle2,
    gradient: 'from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)]',
    bg: 'bg-success/5',
    border: 'border-success/20',
    text: 'text-success',
  },
  not_met: {
    icon: XCircle,
    gradient: 'from-[hsl(0,84%,60%)] to-[hsl(0,72%,50%)]',
    bg: 'bg-destructive/5',
    border: 'border-destructive/20',
    text: 'text-destructive',
  },
  unclear: {
    icon: HelpCircle,
    gradient: 'from-[hsl(38,92%,50%)] to-[hsl(25,95%,53%)]',
    bg: 'bg-warning/5',
    border: 'border-warning/20',
    text: 'text-warning',
  },
  pending: {
    icon: Circle,
    gradient: 'from-muted-foreground to-muted-foreground',
    bg: 'bg-muted/50',
    border: 'border-border',
    text: 'text-muted-foreground',
  },
};

export function PolicyChecklist({ transactionId: _transactionId, className }: PolicyChecklistProps) {
  const criteria: PolicyCriterion[] = [
    { id: 'conservative_therapy', description: '6+ weeks of conservative therapy documented', status: 'met' },
    { id: 'failed_treatment', description: 'Documentation of treatment failure', status: 'met' },
    { id: 'neurological_symptoms', description: 'Red flag symptoms present (optional)', status: 'unclear' },
    { id: 'diagnosis_code', description: 'Valid ICD-10 diagnosis code', status: 'met' },
  ];

  const metCount = criteria.filter(c => c.status === 'met').length;
  const percentage = Math.round((metCount / criteria.length) * 100);

  return (
    <div className={cn('space-y-4', className)}>
      {/* Progress Header */}
      <div className="flex items-center justify-between p-3 rounded-xl bg-gradient-to-r from-primary/5 via-transparent to-accent/5 border border-border/50">
        <div className="flex items-center gap-2">
          <div className="p-1.5 rounded-lg bg-gradient-to-br from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)]">
            <Shield className="h-4 w-4 text-white" />
          </div>
          <span className="font-medium text-sm">Requirements</span>
        </div>
        <span className={cn(
          'text-sm font-bold',
          metCount === criteria.length ? 'text-success' : 'text-foreground'
        )}>
          {metCount}/{criteria.length}
        </span>
      </div>

      {/* Progress Bar */}
      <div className="relative h-2 rounded-full overflow-hidden bg-muted">
        <div 
          className="h-full rounded-full bg-gradient-to-r from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)] transition-all duration-700"
          style={{ width: `${percentage}%` }}
        />
      </div>

      {/* Criteria List */}
      <div className="space-y-2">
        {criteria.map(criterion => {
          const config = statusConfig[criterion.status];
          const Icon = config.icon;

          return (
            <div
              key={criterion.id}
              className={cn(
                'flex items-start gap-3 p-3 rounded-xl border transition-all hover:shadow-glow',
                config.bg,
                config.border
              )}
            >
              <div className={cn('p-1 rounded-lg bg-gradient-to-br mt-0.5', config.gradient)}>
                <Icon className="h-3.5 w-3.5 text-white" />
              </div>
              <p className="text-sm leading-relaxed flex-1">{criterion.description}</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
