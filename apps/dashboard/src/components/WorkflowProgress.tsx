import { cn } from '@/lib/utils';
import { FileText, Radio, Brain, Bell, CheckCircle, Check } from 'lucide-react';

export type WorkflowStep = 'draft' | 'detect' | 'process' | 'deliver' | 'review';
export type StepStatus = 'pending' | 'active' | 'completed' | 'error';

interface WorkflowProgressProps {
  currentStep: WorkflowStep;
  stepStatuses?: Partial<Record<WorkflowStep, StepStatus>>;
  compact?: boolean;
  className?: string;
}

const WORKFLOW_STEPS: { id: WorkflowStep; label: string; description: string; icon: typeof FileText }[] = [
  { id: 'draft', label: 'Draft', description: 'Encounter signed', icon: FileText },
  { id: 'detect', label: 'Detect', description: 'Signature detected', icon: Radio },
  { id: 'process', label: 'Process', description: 'AI extraction', icon: Brain },
  { id: 'deliver', label: 'Deliver', description: 'PA ready', icon: Bell },
  { id: 'review', label: 'Review', description: 'Doctor confirms', icon: CheckCircle },
];

function getStepIndex(step: WorkflowStep): number {
  return WORKFLOW_STEPS.findIndex(s => s.id === step);
}

function getStepStatus(
  stepId: WorkflowStep,
  currentStep: WorkflowStep,
  stepStatuses?: Partial<Record<WorkflowStep, StepStatus>>
): StepStatus {
  if (stepStatuses?.[stepId]) return stepStatuses[stepId]!;
  const stepIndex = getStepIndex(stepId);
  const currentIndex = getStepIndex(currentStep);
  if (stepIndex < currentIndex) return 'completed';
  if (stepIndex === currentIndex) return 'active';
  return 'pending';
}

export function WorkflowProgress({
  currentStep,
  stepStatuses,
  compact = false,
  className,
}: WorkflowProgressProps) {
  return (
    <div className={cn('w-full', className)}>
      <div className="flex items-start justify-between relative">
        {/* Background Line */}
        <div 
          className="absolute h-[2px] bg-border/60 rounded-full"
          style={{ 
            top: compact ? '20px' : '24px', 
            left: '10%', 
            right: '10%',
          }} 
        />
        
        {WORKFLOW_STEPS.map((step, index) => {
          const status = getStepStatus(step.id, currentStep, stepStatuses);
          const Icon = step.icon;
          const prevStatus = index > 0 
            ? getStepStatus(WORKFLOW_STEPS[index - 1].id, currentStep, stepStatuses) 
            : null;

          return (
            <div key={step.id} className="flex flex-col items-center relative z-10 flex-1">
              {/* Progress Line */}
              {index > 0 && (
                <div 
                  className={cn(
                    'absolute h-[2px] rounded-full transition-all duration-500',
                    prevStatus === 'completed' 
                      ? 'bg-gradient-to-r from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)]' 
                      : 'bg-transparent'
                  )}
                  style={{ 
                    top: compact ? '20px' : '24px',
                    right: '50%',
                    width: '100%',
                  }}
                />
              )}
              
              {/* Step Circle */}
              <div
                className={cn(
                  'relative rounded-2xl flex items-center justify-center transition-all duration-300',
                  compact ? 'h-10 w-10' : 'h-12 w-12',
                  status === 'completed' && 'bg-gradient-to-br from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)] text-white shadow-[0_4px_16px_hsl(160,84%,39%,0.3)]',
                  status === 'active' && 'bg-gradient-to-br from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)] text-white shadow-primary ring-4 ring-primary/20',
                  status === 'pending' && 'bg-muted text-muted-foreground',
                  status === 'error' && 'bg-destructive text-white shadow-[0_4px_16px_hsl(0,84%,60%,0.3)]'
                )}
              >
                {status === 'completed' ? (
                  <Check className={compact ? 'h-5 w-5' : 'h-6 w-6'} strokeWidth={2.5} />
                ) : (
                  <Icon className={compact ? 'h-4 w-4' : 'h-5 w-5'} />
                )}
                
                {/* Active Pulse */}
                {status === 'active' && (
                  <div className="absolute inset-0 rounded-2xl bg-gradient-to-br from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)] animate-ping opacity-30" />
                )}
              </div>

              {/* Labels */}
              {!compact && (
                <div className="mt-4 text-center">
                  <p className={cn(
                    'text-sm font-semibold transition-colors',
                    status === 'active' && 'text-primary',
                    status === 'completed' && 'text-success',
                    status === 'pending' && 'text-muted-foreground',
                    status === 'error' && 'text-destructive'
                  )}>
                    {step.label}
                  </p>
                  <p className="text-xs text-muted-foreground mt-0.5 hidden sm:block">
                    {step.description}
                  </p>
                </div>
              )}
              
              {compact && (
                <p className={cn(
                  'text-[10px] font-semibold mt-2 transition-colors',
                  status === 'active' && 'text-primary',
                  status === 'completed' && 'text-success',
                  status === 'pending' && 'text-muted-foreground'
                )}>
                  {step.label}
                </p>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default WorkflowProgress;
