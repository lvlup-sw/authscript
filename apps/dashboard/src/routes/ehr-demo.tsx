import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { EhrHeader, EncounterNote, SignEncounterButton, PAResultsPanel } from '@/components/ehr';
import { useEhrDemoFlow } from '@/components/ehr/useEhrDemoFlow';
import { CriteriaReasonDialog } from './analysis.$transactionId';
import { DEMO_EHR_PATIENT, DEMO_ENCOUNTER } from '@/lib/demoData';
import type { Criterion } from '@/api/graphqlService';

export function EhrDemoPage() {
  const flow = useEhrDemoFlow();
  const [selectedCriterion, setSelectedCriterion] = useState<Criterion | null>(null);

  return (
    <div className="fixed inset-0 z-[100] min-h-screen bg-gray-100 overflow-auto">
      <EhrHeader patient={DEMO_EHR_PATIENT} />
      <div className="max-w-5xl mx-auto p-6 space-y-6">
        <EncounterNote encounter={DEMO_ENCOUNTER} />
        <div className="flex justify-end">
          <SignEncounterButton onSign={() => flow.sign()} signed={flow.state !== 'idle'} />
        </div>
        {flow.state !== 'idle' && (
          <div className="animate-fade-slide-in">
            <PAResultsPanel
              state={flow.state}
              paRequest={flow.paRequest}
              error={flow.error}
              onSubmit={() => flow.submit()}
              onCriterionClick={(c) => setSelectedCriterion(c)}
            />
          </div>
        )}
      </div>

      {/* Criteria reason dialog */}
      <CriteriaReasonDialog
        isOpen={!!selectedCriterion}
        onClose={() => setSelectedCriterion(null)}
        met={selectedCriterion?.met ?? null}
        label={selectedCriterion?.label ?? ''}
        reason={selectedCriterion?.reason ?? ''}
      />
    </div>
  );
}

export const Route = createFileRoute('/ehr-demo')({
  component: EhrDemoPage,
});
