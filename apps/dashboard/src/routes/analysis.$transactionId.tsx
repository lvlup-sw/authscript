import { createFileRoute } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { EvidencePanel } from '@/components/EvidencePanel';
import { PolicyChecklist } from '@/components/PolicyChecklist';
import { FormPreview } from '@/components/FormPreview';
import { ConfidenceMeter } from '@/components/ConfidenceMeter';
import { authscriptService } from '@/api/authscriptService';

export const Route = createFileRoute('/analysis/$transactionId')({
  component: AnalysisPage,
});

function AnalysisPage() {
  const { transactionId } = Route.useParams();

  // Fetch analysis result containing evidence and form data
  const { data: analysisResult, isLoading } = useQuery({
    queryKey: ['analysis', transactionId],
    queryFn: () => authscriptService.getAnalysisResult(transactionId),
    enabled: Boolean(transactionId),
  });

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Analysis Detail</h1>
        <p className="text-muted-foreground mt-2">
          Transaction: <code className="bg-muted px-2 py-1 rounded">{transactionId}</code>
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Extracted Evidence</CardTitle>
              <CardDescription>
                Clinical evidence extracted from patient records
              </CardDescription>
            </CardHeader>
            <CardContent>
              <EvidencePanel
                evidence={analysisResult?.supportingEvidence ?? []}
                loading={isLoading}
              />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Form Preview</CardTitle>
              <CardDescription>
                Side-by-side comparison of blank template and AI-filled form
              </CardDescription>
            </CardHeader>
            <CardContent>
              <FormPreview
                fieldMappings={analysisResult?.fieldMappings ?? {}}
                loading={isLoading}
              />
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>AI Confidence</CardTitle>
            </CardHeader>
            <CardContent>
              <ConfidenceMeter score={analysisResult?.confidenceScore ?? 0} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Policy Checklist</CardTitle>
              <CardDescription>
                MRI Lumbar Spine - Blue Cross
              </CardDescription>
            </CardHeader>
            <CardContent>
              <PolicyChecklist transactionId={transactionId} />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
