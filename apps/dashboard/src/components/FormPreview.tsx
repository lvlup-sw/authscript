import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Download } from 'lucide-react';

interface FormPreviewProps {
  fieldMappings: Record<string, string>;
  loading?: boolean;
  showHighlights?: boolean;
  onDownload?: () => void;
  className?: string;
}

/**
 * Format field name from snake_case to Title Case
 */
function formatFieldName(name: string): string {
  return name
    .split('_')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

/**
 * Check if field is a special long-form field
 */
function isLongFormField(name: string): boolean {
  return ['clinical_summary', 'justification', 'notes'].includes(name);
}

/**
 * Form Preview Component
 * Displays filled PA form fields with optional highlighting and download
 */
export function FormPreview({
  fieldMappings,
  loading = false,
  showHighlights = true,
  onDownload,
  className,
}: FormPreviewProps) {
  const fields = Object.entries(fieldMappings);
  const hasFields = fields.length > 0;

  // Loading state
  if (loading) {
    return (
      <Card className={className} data-testid="form-skeleton">
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent className="space-y-4">
          {[1, 2, 3, 4, 5].map(i => (
            <div key={i} className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-8 w-full" />
            </div>
          ))}
        </CardContent>
      </Card>
    );
  }

  // Empty state
  if (!hasFields) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle>PA Form Preview</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground">No form data available</p>
        </CardContent>
      </Card>
    );
  }

  // Separate regular fields from long-form fields
  const regularFields = fields.filter(([key]) => !isLongFormField(key));
  const longFormFields = fields.filter(([key]) => isLongFormField(key));

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>PA Form Preview</CardTitle>
          {onDownload && (
            <Button
              variant="outline"
              size="sm"
              onClick={onDownload}
              disabled={loading}
            >
              <Download className="h-4 w-4 mr-2" />
              Download PDF
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Regular fields in grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {regularFields.map(([key, value]) => (
            <div
              key={key}
              data-field={key}
              className={cn(
                'p-3 rounded-lg border',
                showHighlights && value && 'bg-green-50 border-green-200'
              )}
            >
              <p className="text-xs font-medium text-muted-foreground mb-1">
                {formatFieldName(key)}
              </p>
              <p className="text-sm font-medium">{value || '—'}</p>
            </div>
          ))}
        </div>

        {/* Long-form fields (clinical summary, etc.) */}
        {longFormFields.map(([key, value]) => (
          <div
            key={key}
            data-field={key}
            className={cn(
              'p-4 rounded-lg border',
              showHighlights && value && 'bg-green-50 border-green-200'
            )}
          >
            <p className="text-xs font-medium text-muted-foreground mb-2">
              {formatFieldName(key)}
            </p>
            <p className="text-sm whitespace-pre-wrap">{value || '—'}</p>
          </div>
        ))}

        {/* Highlight legend */}
        {showHighlights && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground pt-4 border-t">
            <div className="w-3 h-3 rounded bg-green-200 border border-green-300" />
            <span>AI-filled fields</span>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default FormPreview;
