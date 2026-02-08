import { Link } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { WorkflowProgress, type WorkflowStep, type StepStatus } from './WorkflowProgress';
import { 
  Clock, 
  User, 
  FileText, 
  AlertTriangle, 
  CheckCircle2,
  ArrowRight,
  Building2,
  Shield,
  Sparkles
} from 'lucide-react';

export interface PARequest {
  id: string;
  patientName: string;
  patientId: string;
  procedureCode: string;
  procedureName: string;
  payer: string;
  currentStep: WorkflowStep;
  stepStatuses?: Partial<Record<WorkflowStep, StepStatus>>;
  confidenceScore?: number;
  createdAt: string;
  encounterId: string;
  requiresAttention?: boolean;
}

interface PARequestCardProps {
  request: PARequest;
  onReview?: (requestId: string) => void;
  className?: string;
}

function formatTimeAgo(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  return `${Math.floor(diffHours / 24)}d ago`;
}

function ConfidenceBadge({ score }: { score: number | undefined }) {
  if (score === undefined) return null;

  if (score >= 0.8) {
    return (
      <Badge className="bg-success/10 text-success border-success/30 gap-1.5">
        <Shield className="h-3 w-3" />
        {Math.round(score * 100)}%
      </Badge>
    );
  }
  if (score >= 0.5) {
    return (
      <Badge className="bg-warning/10 text-warning border-warning/30 gap-1.5">
        <Sparkles className="h-3 w-3" />
        {Math.round(score * 100)}%
      </Badge>
    );
  }
  return (
    <Badge className="bg-destructive/10 text-destructive border-destructive/30 gap-1.5">
      <AlertTriangle className="h-3 w-3" />
      Review
    </Badge>
  );
}

export function PARequestCard({ request, onReview, className }: PARequestCardProps) {
  const isReadyForReview = request.currentStep === 'deliver' || request.currentStep === 'review';
  const isCompleted = request.currentStep === 'review' && request.stepStatuses?.review === 'completed';

  return (
    <Card className={cn(
      'group relative overflow-hidden border-0 shadow-glow hover:shadow-glow-lg transition-all duration-300',
      isReadyForReview && !isCompleted && 'border-gradient',
      request.requiresAttention && 'ring-1 ring-warning/30',
      className
    )}>
      {/* Top Gradient Bar */}
      <div className={cn(
        'absolute top-0 left-0 right-0 h-1',
        isReadyForReview && !isCompleted && 'bg-gradient-to-r from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)]',
        isCompleted && 'bg-gradient-to-r from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)]',
        request.requiresAttention && 'bg-warning',
        !isReadyForReview && !isCompleted && !request.requiresAttention && 'bg-gradient-to-r from-[hsl(172,66%,50%)] to-[hsl(199,89%,48%)]'
      )} />

      <CardContent className="p-5 pt-6">
        {/* Header */}
        <div className="flex items-start justify-between gap-4 mb-4">
          <div className="flex items-center gap-3">
            <div className={cn(
              'w-11 h-11 rounded-xl flex items-center justify-center transition-transform group-hover:scale-105',
              isCompleted 
                ? 'bg-gradient-to-br from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)] shadow-[0_4px_12px_hsl(160,84%,39%,0.2)]' 
                : 'bg-gradient-to-br from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)] shadow-primary'
            )}>
              <User className="h-5 w-5 text-white" />
            </div>
            <div>
              <h3 className="font-semibold text-base">{request.patientName}</h3>
              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                <span className="font-mono bg-muted px-1.5 py-0.5 rounded">MRN: {request.patientId}</span>
                <span className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  {formatTimeAgo(request.createdAt)}
                </span>
              </div>
            </div>
          </div>
          
          <ConfidenceBadge score={request.confidenceScore} />
        </div>

        {/* Procedure & Payer */}
        <div className="flex flex-wrap items-center gap-3 mb-5 pb-4 border-b border-border/50">
          <div className="flex items-center gap-2 text-sm">
            <FileText className="h-4 w-4 text-muted-foreground" />
            <span className="font-semibold">{request.procedureCode}</span>
            <span className="text-muted-foreground">—</span>
            <span className="text-muted-foreground truncate max-w-[160px]">{request.procedureName}</span>
          </div>
          <Badge variant="outline" className="ml-auto gap-1.5 bg-card">
            <Building2 className="h-3 w-3" />
            {request.payer}
          </Badge>
        </div>

        {/* Workflow Progress */}
        <div className="mb-5">
          <WorkflowProgress
            currentStep={request.currentStep}
            stepStatuses={request.stepStatuses}
            compact
          />
        </div>

        {/* Actions */}
        {isReadyForReview && !isCompleted && (
          <div className="flex items-center gap-3 pt-4 border-t border-border/50">
            <Button
              onClick={() => onReview?.(request.id)}
              className="flex-1 bg-gradient-to-r from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)] hover:opacity-90 border-0 shadow-primary transition-all"
            >
              <CheckCircle2 className="h-4 w-4 mr-2" />
              Review & Confirm
            </Button>
            <Link to="/analysis/$transactionId" params={{ transactionId: request.id }}>
              <Button variant="outline" size="icon" className="shadow-glow hover:shadow-glow-lg">
                <ArrowRight className="h-4 w-4" />
              </Button>
            </Link>
          </div>
        )}

        {isCompleted && (
          <div className="flex items-center gap-3 pt-4 border-t border-border/50">
            <div className="flex items-center gap-2 text-sm text-success flex-1">
              <div className="p-1.5 rounded-lg bg-success/10">
                <CheckCircle2 className="h-4 w-4" />
              </div>
              <span className="font-medium">Submitted to athenahealth</span>
            </div>
            <Link to="/analysis/$transactionId" params={{ transactionId: request.id }}>
              <Button variant="ghost" size="sm" className="text-muted-foreground hover:text-foreground">
                Details
                <ArrowRight className="h-3 w-3 ml-1" />
              </Button>
            </Link>
          </div>
        )}

        {!isReadyForReview && !isCompleted && (
          <div className="flex items-center justify-between pt-4 border-t border-border/50">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <div className="relative">
                <span className="flex h-2 w-2 rounded-full bg-accent" />
                <span className="absolute inset-0 h-2 w-2 rounded-full bg-accent animate-ping" />
              </div>
              <span>Processing...</span>
            </div>
            <Link to="/analysis/$transactionId" params={{ transactionId: request.id }}>
              <Button variant="ghost" size="sm" className="text-muted-foreground hover:text-foreground">
                View Progress
                <ArrowRight className="h-3 w-3 ml-1" />
              </Button>
            </Link>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default PARequestCard;
