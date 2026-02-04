import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import type { EvidenceItem } from '@authscript/types';
import { CheckCircle2, XCircle, HelpCircle, FileText, Sparkles, TrendingUp } from 'lucide-react';

interface EvidencePanelProps {
  evidence: EvidenceItem[];
  loading?: boolean;
  className?: string;
}

const statusConfig = {
  MET: {
    icon: CheckCircle2,
    gradient: 'from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)]',
    bg: 'bg-success/5',
    border: 'border-success/20',
    text: 'text-success',
    badge: 'bg-success/10 text-success border-success/30',
    label: 'Met',
  },
  NOT_MET: {
    icon: XCircle,
    gradient: 'from-[hsl(0,84%,60%)] to-[hsl(0,72%,50%)]',
    bg: 'bg-destructive/5',
    border: 'border-destructive/20',
    text: 'text-destructive',
    badge: 'bg-destructive/10 text-destructive border-destructive/30',
    label: 'Not Met',
  },
  UNCLEAR: {
    icon: HelpCircle,
    gradient: 'from-[hsl(38,92%,50%)] to-[hsl(25,95%,53%)]',
    bg: 'bg-warning/5',
    border: 'border-warning/20',
    text: 'text-warning',
    badge: 'bg-warning/10 text-warning border-warning/30',
    label: 'Unclear',
  },
};

function formatCriterionId(id: string): string {
  return id.split('_').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
}

export function EvidencePanel({ evidence, loading, className }: EvidencePanelProps) {
  if (loading) {
    return (
      <div className={cn('space-y-4', className)}>
        {[1, 2, 3].map(i => (
          <div key={i} className="p-4 rounded-xl border border-border/50 space-y-3">
            <div className="flex justify-between">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-5 w-16" />
            </div>
            <Skeleton className="h-12 w-full" />
            <div className="flex justify-between">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-20" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (!evidence?.length) {
    return (
      <div className={cn('text-center py-12', className)}>
        <div className="w-14 h-14 rounded-2xl bg-muted flex items-center justify-center mx-auto mb-4">
          <FileText className="h-7 w-7 text-muted-foreground" />
        </div>
        <p className="text-muted-foreground">No evidence extracted yet</p>
      </div>
    );
  }

  const counts = {
    met: evidence.filter(e => e.status === 'MET').length,
    notMet: evidence.filter(e => e.status === 'NOT_MET').length,
    unclear: evidence.filter(e => e.status === 'UNCLEAR').length,
  };

  return (
    <div className={cn('space-y-4', className)}>
      {/* Summary */}
      <div className="flex items-center justify-between p-4 rounded-xl bg-gradient-to-r from-primary/5 via-transparent to-accent/5 border border-border/50">
        <div className="flex items-center gap-2">
          <div className="p-1.5 rounded-lg bg-gradient-to-br from-[hsl(243,75%,59%)] to-[hsl(280,75%,55%)]">
            <Sparkles className="h-4 w-4 text-white" />
          </div>
          <span className="font-medium">{evidence.length} criteria analyzed</span>
        </div>
        <div className="flex items-center gap-4 text-sm">
          <span className="flex items-center gap-1.5 text-success">
            <CheckCircle2 className="h-4 w-4" />
            {counts.met}
          </span>
          <span className="flex items-center gap-1.5 text-destructive">
            <XCircle className="h-4 w-4" />
            {counts.notMet}
          </span>
          <span className="flex items-center gap-1.5 text-warning">
            <HelpCircle className="h-4 w-4" />
            {counts.unclear}
          </span>
        </div>
      </div>

      {/* Evidence Items */}
      <div className="space-y-3">
        {evidence.map((item, index) => {
          const config = statusConfig[item.status] || statusConfig.UNCLEAR;
          const Icon = config.icon;

          return (
            <div
              key={`${item.criterionId}-${index}`}
              className={cn(
                'p-4 rounded-xl border transition-all hover:shadow-glow',
                config.bg,
                config.border
              )}
            >
              <div className="flex items-start justify-between gap-4 mb-3">
                <div className="flex items-center gap-2.5">
                  <div className={cn('p-1.5 rounded-lg bg-gradient-to-br', config.gradient)}>
                    <Icon className="h-4 w-4 text-white" />
                  </div>
                  <h4 className="font-semibold text-sm">{formatCriterionId(item.criterionId)}</h4>
                </div>
                <Badge className={config.badge}>{config.label}</Badge>
              </div>

              <p className="text-sm text-foreground/80 mb-3 leading-relaxed">{item.evidence}</p>

              <div className="flex items-center justify-between text-xs">
                <span className="text-muted-foreground flex items-center gap-1.5">
                  <FileText className="h-3 w-3" />
                  {item.source}
                </span>
                <span className={cn(
                  'font-medium flex items-center gap-1',
                  item.confidence >= 0.8 ? 'text-success' : item.confidence >= 0.5 ? 'text-warning' : 'text-destructive'
                )}>
                  <TrendingUp className="h-3 w-3" />
                  {Math.round(item.confidence * 100)}%
                </span>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default EvidencePanel;
