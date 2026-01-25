import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import type { EvidenceItem } from '@authscript/types';

interface EvidencePanelProps {
  evidence: EvidenceItem[];
  loading?: boolean;
  className?: string;
}

/**
 * Get badge color class based on evidence status
 */
function getStatusBadgeClass(status: EvidenceItem['status']): string {
  switch (status) {
    case 'MET':
      return 'bg-green-500 hover:bg-green-600';
    case 'NOT_MET':
      return 'bg-red-500 hover:bg-red-600';
    case 'UNCLEAR':
      return 'bg-yellow-500 hover:bg-yellow-600';
    default:
      return 'bg-gray-500';
  }
}

/**
 * Format confidence as percentage
 */
function formatConfidence(confidence: number): string {
  return `${Math.round(confidence * 100)}%`;
}

/**
 * Format criterion ID to display name
 */
function formatCriterionId(id: string): string {
  return id
    .split('_')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

/**
 * Evidence Panel Component
 * Displays extracted policy evidence with status badges and confidence scores
 */
export function EvidencePanel({ evidence, loading, className }: EvidencePanelProps) {
  // Loading state
  if (loading) {
    return (
      <Card className={className} data-testid="evidence-skeleton">
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent className="space-y-4">
          {[1, 2, 3].map(i => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  // Empty state
  if (!evidence || evidence.length === 0) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle>Policy Evidence</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground">No evidence extracted yet</p>
        </CardContent>
      </Card>
    );
  }

  // Calculate summary counts
  const metCount = evidence.filter(e => e.status === 'MET').length;
  const notMetCount = evidence.filter(e => e.status === 'NOT_MET').length;
  const unclearCount = evidence.filter(e => e.status === 'UNCLEAR').length;

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Policy Evidence</CardTitle>
          <div className="flex gap-2 text-sm">
            <span className="text-green-600">{metCount} met</span>
            <span className="text-muted-foreground">|</span>
            <span className="text-red-600">{notMetCount} not met</span>
            <span className="text-muted-foreground">|</span>
            <span className="text-yellow-600">{unclearCount} unclear</span>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {evidence.map((item, index) => (
          <div
            key={`${item.criterionId}-${index}`}
            className="border rounded-lg p-4 space-y-2"
          >
            {/* Header: Criterion + Status */}
            <div className="flex items-center justify-between">
              <h4 className="font-medium">
                {formatCriterionId(item.criterionId)}
                <span className="sr-only">{item.criterionId}</span>
              </h4>
              <Badge className={cn('text-white', getStatusBadgeClass(item.status))}>
                {item.status}
              </Badge>
            </div>

            {/* Evidence text */}
            <p className="text-sm text-foreground">{item.evidence}</p>

            {/* Footer: Source + Confidence */}
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span>Source: {item.source}</span>
              <span>Confidence: {formatConfidence(item.confidence)}</span>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

export default EvidencePanel;
