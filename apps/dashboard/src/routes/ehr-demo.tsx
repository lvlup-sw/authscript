import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { FileText, ClipboardList } from 'lucide-react';
import {
  EhrHeader,
  EncounterNote,
  EncounterSidebar,
  SignEncounterButton,
  PAResultsPanel,
} from '@/components/ehr';
import { useEhrDemoFlow } from '@/components/ehr/useEhrDemoFlow';
import { CriteriaReasonDialog } from './analysis.$transactionId';
import { PdfViewerModal } from '@/components/PdfViewerModal';
import { PolicyCriteriaModal } from '@/components/ehr/PolicyCriteriaModal';
import {
  DEMO_EHR_PATIENT,
  DEMO_ENCOUNTER,
  DEMO_ENCOUNTER_META,
  DEMO_VITALS,
  DEMO_ORDERS,
} from '@/lib/demoData';
import type { Criterion } from '@/api/graphqlService';
import type { Order } from '@/components/ehr/EncounterNote';

const BLANK_PA_FORM_URL = '/pdf-templates/tx-standard-pa-form.pdf';

function deriveOrderStatus(flowState: string, baseStatus: Order['status']): Order['status'] {
  if (flowState === 'idle') return baseStatus;
  if (flowState === 'signing' || flowState === 'processing') return 'pending';
  return 'completed';
}

export function EhrDemoPage() {
  const flow = useEhrDemoFlow();
  const [selectedCriterion, setSelectedCriterion] = useState<Criterion | null>(null);
  const [pdfOpen, setPdfOpen] = useState(false);
  const [blankFormOpen, setBlankFormOpen] = useState(false);
  const [policyCriteriaOpen, setPolicyCriteriaOpen] = useState(false);

  const dynamicOrders = DEMO_ORDERS.map(order => ({
    ...order,
    status: deriveOrderStatus(flow.state, order.status),
  }));

  return (
    <div className="fixed inset-0 z-[100] min-h-screen bg-gray-100 overflow-auto">
      <EhrHeader patient={DEMO_EHR_PATIENT} encounterMeta={DEMO_ENCOUNTER_META} />
      <div className="flex">
        <EncounterSidebar signed={flow.state !== 'idle'} flowState={flow.state} />
        <div className="flex-1 max-w-5xl mx-auto p-6 space-y-6">
          <EncounterNote
            encounter={DEMO_ENCOUNTER}
            vitals={DEMO_VITALS}
            orders={dynamicOrders}
          />
          <div className="flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={() => setBlankFormOpen(true)}
              className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm transition-colors hover:bg-gray-50"
            >
              <FileText className="h-4 w-4" aria-hidden="true" />
              Preview PA Form
            </button>
            <button
              type="button"
              onClick={() => setPolicyCriteriaOpen(true)}
              className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm transition-colors hover:bg-gray-50"
            >
              <ClipboardList className="h-4 w-4" aria-hidden="true" />
              View Policy Criteria
            </button>
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
                onViewPdf={() => setPdfOpen(true)}
              />
            </div>
          )}
        </div>
      </div>

      {/* Criteria reason dialog */}
      <CriteriaReasonDialog
        isOpen={!!selectedCriterion}
        onClose={() => setSelectedCriterion(null)}
        met={selectedCriterion?.met ?? null}
        label={selectedCriterion?.label ?? ''}
        reason={selectedCriterion?.reason ?? ''}
      />

      {/* Filled PDF viewer modal (post-sign) */}
      <PdfViewerModal
        isOpen={pdfOpen}
        onClose={() => setPdfOpen(false)}
        request={flow.paRequest}
      />

      {/* Blank PA form viewer (pre-sign) */}
      <PdfViewerModal
        isOpen={blankFormOpen}
        onClose={() => setBlankFormOpen(false)}
        staticUrl={BLANK_PA_FORM_URL}
        title="TX Standard PA Request Form (NOFR001)"
      />

      {/* Policy criteria modal */}
      <PolicyCriteriaModal
        isOpen={policyCriteriaOpen}
        onClose={() => setPolicyCriteriaOpen(false)}
      />
    </div>
  );
}

export const Route = createFileRoute('/ehr-demo')({
  component: EhrDemoPage,
});
