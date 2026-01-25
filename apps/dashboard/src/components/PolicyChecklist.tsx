import { CheckCircle, XCircle, HelpCircle } from 'lucide-react';

interface PolicyCriterion {
  id: string;
  description: string;
  status: 'met' | 'not_met' | 'unclear' | 'pending';
}

interface PolicyChecklistProps {
  transactionId: string;
}

export function PolicyChecklist({ transactionId: _transactionId }: PolicyChecklistProps) {
  // MRI Lumbar Spine - Blue Cross policy criteria
  const criteria: PolicyCriterion[] = [
    {
      id: 'conservative_therapy',
      description: '6+ weeks of conservative therapy',
      status: 'pending',
    },
    {
      id: 'failed_treatment',
      description: 'Documentation of treatment failure',
      status: 'pending',
    },
    {
      id: 'neurological_symptoms',
      description: 'Red flag neurological symptoms (optional bypass)',
      status: 'pending',
    },
    {
      id: 'diagnosis_code',
      description: 'Valid ICD-10 diagnosis code (M54.5 or similar)',
      status: 'pending',
    },
  ];

  return (
    <div className="space-y-3">
      {criteria.map(criterion => (
        <div
          key={criterion.id}
          className="flex items-start gap-3 p-3 rounded-lg border bg-card"
        >
          <div className="flex-shrink-0 mt-0.5">
            <StatusIcon status={criterion.status} />
          </div>
          <div className="flex-1">
            <p className="text-sm">{criterion.description}</p>
          </div>
        </div>
      ))}
    </div>
  );
}

function StatusIcon({ status }: { status: PolicyCriterion['status'] }) {
  switch (status) {
    case 'met':
      return <CheckCircle className="h-5 w-5 text-green-500" />;
    case 'not_met':
      return <XCircle className="h-5 w-5 text-destructive" />;
    case 'unclear':
      return <HelpCircle className="h-5 w-5 text-amber-500" />;
    case 'pending':
      return (
        <div className="h-5 w-5 rounded-full border-2 border-muted-foreground" />
      );
  }
}
