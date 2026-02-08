import { cn } from '@/lib/utils';
import { Skeleton } from '@/components/ui/skeleton';
import { FileText, CheckCircle2, Sparkles, Edit3 } from 'lucide-react';

interface FormPreviewProps {
  fieldMappings: Record<string, string>;
  loading?: boolean;
  showHighlights?: boolean;
  className?: string;
}

function formatFieldName(name: string): string {
  return name.split('_').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
}

function isLongFormField(name: string): boolean {
  return ['clinical_summary', 'justification', 'notes', 'medical_necessity'].includes(name);
}

export function FormPreview({
  fieldMappings,
  loading = false,
  showHighlights = true,
  className,
}: FormPreviewProps) {
  const fields = Object.entries(fieldMappings);

  if (loading) {
    return (
      <div className={cn('space-y-4', className)} data-testid="form-skeleton">
        <div className="grid grid-cols-2 gap-4">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <div key={i} className="space-y-2">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-12 w-full rounded-xl" />
            </div>
          ))}
        </div>
        <Skeleton className="h-28 w-full rounded-xl" />
      </div>
    );
  }

  if (!fields.length) {
    return (
      <div className={cn('text-center py-12', className)}>
        <div className="w-14 h-14 rounded-2xl bg-muted flex items-center justify-center mx-auto mb-4">
          <FileText className="h-7 w-7 text-muted-foreground" />
        </div>
        <p className="text-muted-foreground">No form data available</p>
      </div>
    );
  }

  const regularFields = fields.filter(([key]) => !isLongFormField(key));
  const longFormFields = fields.filter(([key]) => isLongFormField(key));

  return (
    <div className={cn('space-y-6', className)}>
      {/* Header */}
      <div className="flex items-center justify-between p-4 rounded-xl bg-gradient-to-r from-success/5 via-transparent to-accent/5 border border-success/20">
        <div className="flex items-center gap-2">
          <div className="p-1.5 rounded-lg bg-gradient-to-br from-[hsl(160,84%,39%)] to-[hsl(172,66%,50%)]">
            <Sparkles className="h-4 w-4 text-white" />
          </div>
          <span className="font-medium">{fields.length} fields populated</span>
        </div>
        <div className="flex items-center gap-1.5 text-sm text-success">
          <CheckCircle2 className="h-4 w-4" />
          AI auto-filled
        </div>
      </div>

      {/* Regular Fields */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {regularFields.map(([key, value]) => (
          <div
            key={key}
            data-field
            data-highlighted={showHighlights && !!value ? 'true' : undefined}
            className={cn(
              'p-4 rounded-xl border transition-all group hover:shadow-glow',
              showHighlights && value 
                ? 'bg-success/5 border-success/20 hover:border-success/40' 
                : 'bg-muted/30 border-border/50 hover:border-border'
            )}
          >
            <div className="flex items-center justify-between mb-2">
              <p className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
                {formatFieldName(key)}
              </p>
              {showHighlights && value && (
                <Edit3 className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
              )}
            </div>
            <p className="text-sm font-medium">
              {value || <span className="text-muted-foreground italic">Not provided</span>}
            </p>
          </div>
        ))}
      </div>

      {/* Long Form Fields */}
      {longFormFields.map(([key, value]) => (
        <div
          key={key}
          data-field
          data-highlighted={showHighlights && !!value ? 'true' : undefined}
          className={cn(
            'p-5 rounded-xl border transition-all group hover:shadow-glow',
            showHighlights && value 
              ? 'bg-success/5 border-success/20 hover:border-success/40' 
              : 'bg-muted/30 border-border/50 hover:border-border'
          )}
        >
          <div className="flex items-center justify-between mb-3">
            <p className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
              {formatFieldName(key)}
            </p>
            {showHighlights && value && (
              <Edit3 className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
            )}
          </div>
          <p className="text-sm whitespace-pre-wrap leading-relaxed">
            {value || <span className="text-muted-foreground italic">Not provided</span>}
          </p>
        </div>
      ))}

      {/* Legend */}
      {showHighlights && (
        <div className="flex items-center gap-6 pt-4 border-t border-border/50 text-xs text-muted-foreground">
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 rounded-md bg-success/20 border border-success/40" />
            <span>AI-populated</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 rounded-md bg-muted border border-border" />
            <span>Empty</span>
          </div>
        </div>
      )}
    </div>
  );
}

export default FormPreview;
