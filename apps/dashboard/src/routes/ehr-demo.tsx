import { useState, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { createFileRoute } from '@tanstack/react-router';
import {
  FileText,
  X,
  CheckCircle2,
  Shield,
  Clock,
  User,
  Building2,
  Hash,
  Stethoscope,
} from 'lucide-react';
import {
  EhrHeader,
  EncounterNote,
  EncounterSidebar,
  SignEncounterButton,
  PAReadinessWidget,
  PAResultsPanel,
} from '@/components/ehr';
import { useEhrDemoFlow } from '@/components/ehr/useEhrDemoFlow';
import { CriteriaReasonDialog } from './analysis.$transactionId';
import { PdfViewerModal } from '@/components/PdfViewerModal';
import {
  DEMO_EHR_PATIENT,
  DEMO_ENCOUNTER_META,
  DEMO_ORDERS,
  DEMO_HPI_ADDENDUM,
  DEMO_ASSESSMENT_ADDENDUM,
} from '@/lib/demoData';


import type { Criterion, PARequest } from '@/api/graphqlService';
import type { Order } from '@/components/ehr/EncounterNote';

const BLANK_PA_FORM_URL = '/pdf-templates/tx-standard-pa-form.pdf';

function deriveOrderStatus(flowState: string, baseStatus: Order['status']): Order['status'] {
  if (flowState === 'idle' || flowState === 'flagged') return baseStatus;
  if (flowState === 'signing' || flowState === 'processing') return 'pending';
  return 'completed';
}

/** Submission confirmation receipt dialog */
function ConfirmationDialog({
  isOpen,
  onClose,
  paRequest,
}: {
  isOpen: boolean;
  onClose: () => void;
  paRequest: PARequest | null;
}) {
  if (!isOpen || !paRequest) return null;

  const confirmationId = `CONF-${paRequest.id.replace('PA-DEMO-', '')}`;
  const submittedDate = paRequest.submittedAt
    ? new Date(paRequest.submittedAt)
    : new Date();

  const rows: { icon: React.ReactNode; label: string; value: string }[] = [
    {
      icon: <Hash className="h-4 w-4 text-slate-400" />,
      label: 'Confirmation #',
      value: confirmationId,
    },
    {
      icon: <Hash className="h-4 w-4 text-slate-400" />,
      label: 'PA Request ID',
      value: paRequest.id,
    },
    {
      icon: <Clock className="h-4 w-4 text-slate-400" />,
      label: 'Submitted',
      value: submittedDate.toLocaleString(),
    },
    {
      icon: <User className="h-4 w-4 text-slate-400" />,
      label: 'Patient',
      value: `${paRequest.patient?.name ?? 'Unknown'} (MRN: ${paRequest.patient?.mrn ?? 'N/A'})`,
    },
    {
      icon: <Building2 className="h-4 w-4 text-slate-400" />,
      label: 'Payer',
      value: paRequest.payer,
    },
    {
      icon: <Stethoscope className="h-4 w-4 text-slate-400" />,
      label: 'Procedure',
      value: `${paRequest.procedureCode} — ${paRequest.procedureName}`,
    },
    {
      icon: <User className="h-4 w-4 text-slate-400" />,
      label: 'Ordering Provider',
      value: `${paRequest.provider} (NPI: ${paRequest.providerNpi})`,
    },
    {
      icon: <Shield className="h-4 w-4 text-slate-400" />,
      label: 'Policy',
      value: 'LCD L34220 — MRI Lumbar Spine',
    },
  ];

  return createPortal(
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-md mx-4 overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between border-b border-gray-200 px-5 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-green-100">
              <CheckCircle2 className="h-4 w-4 text-green-600" />
            </div>
            <div>
              <div className="text-sm font-semibold text-gray-900">
                Submission Confirmation
              </div>
              <div className="text-xs text-gray-500">
                athenahealth DocumentReference
              </div>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 transition-colors hover:bg-gray-100"
          >
            <X className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Receipt rows */}
        <div className="divide-y divide-gray-100 px-5">
          {rows.map((row) => (
            <div key={row.label} className="flex items-start gap-3 py-3">
              <span className="mt-0.5">{row.icon}</span>
              <div className="min-w-0 flex-1">
                <div className="text-xs font-medium uppercase tracking-wider text-gray-400">
                  {row.label}
                </div>
                <div className="mt-0.5 text-sm text-gray-900 break-words">
                  {row.value}
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="border-t border-gray-200 bg-slate-50 px-5 py-3">
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <Shield className="h-3.5 w-3.5" />
            <span>
              PA form written to patient chart via{' '}
              <span className="font-mono">POST /fhir/r4/DocumentReference</span>
            </span>
          </div>
        </div>
      </div>
    </div>,
    document.body,
  );
}

export function EhrDemoPage() {
  const flow = useEhrDemoFlow();
  const [selectedCriterion, setSelectedCriterion] = useState<Criterion | null>(null);
  const [pdfOpen, setPdfOpen] = useState(false);
  const [blankFormOpen, setBlankFormOpen] = useState(false);
  const [confirmationOpen, setConfirmationOpen] = useState(false);

  // Auto-trigger PA detection on mount
  useEffect(() => {
    flow.flag();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const dynamicOrders = DEMO_ORDERS.map(order => ({
    ...order,
    status: deriveOrderStatus(flow.state, order.status),
  }));

  const preCheckCount = flow.preCheckCriteria
    ? { met: flow.preCheckCriteria.filter(c => c.status === 'met').length, total: flow.preCheckCriteria.length }
    : undefined;

  const isSigned = flow.state !== 'idle' && flow.state !== 'flagged';
  const showPostSignPanel = isSigned && flow.state !== 'error';

  // Show highlight only after text has been inserted into the note
  const textInserted = flow.docState === 'inserted' || flow.docState === 'saving' || flow.docState === 'saved';

  return (
    <div className="fixed inset-0 z-[100] min-h-screen bg-gray-100 overflow-auto">
      <EhrHeader patient={DEMO_EHR_PATIENT} encounterMeta={DEMO_ENCOUNTER_META} />
      <div className="flex">
        <EncounterSidebar signed={isSigned} flowState={flow.state} preCheckCount={preCheckCount} />
        <div className="flex-1 max-w-5xl mx-auto p-6 space-y-6">
          <EncounterNote
            encounter={flow.encounter}
            vitals={{bp: '128/82', hr: 72, temp: 98.6, spo2: 99}}
            orders={dynamicOrders}
            hpiHighlight={textInserted ? (flow.insertedHpiText ?? DEMO_HPI_ADDENDUM) : undefined}
            assessmentHighlight={textInserted ? DEMO_ASSESSMENT_ADDENDUM : undefined}
            docState={flow.docState}
            suggestionText={DEMO_HPI_ADDENDUM}
            onInsertSuggestion={(editedText) => flow.insertToNote(editedText)}
            onSaveToChart={() => flow.saveToChart()}
          />

          {/* Pre-check readiness widget (visible in flagged state) */}
          {flow.state === 'flagged' && flow.preCheckCriteria && (
            <div className="animate-fade-slide-in">
              <PAReadinessWidget
                state="ready"
                criteria={flow.preCheckCriteria}
                order={{ code: '72148', name: 'MRI Lumbar Spine w/o Contrast' }}
                payer="Blue Cross Blue Shield"
                policyId="LCD L34220"
                onGapAction={() => flow.openSuggestion()}
                docState={flow.docState}
              />
            </div>
          )}

          <div className="flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={() => setBlankFormOpen(true)}
              className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm transition-colors hover:bg-gray-50"
            >
              <FileText className="h-4 w-4" aria-hidden="true" />
              Preview PA Form
            </button>
            <SignEncounterButton onSign={() => flow.sign()} signed={isSigned} />
          </div>

          {/* Post-sign PA results panel */}
          {showPostSignPanel && (
            <div className="animate-fade-slide-in">
              <PAResultsPanel
                state={flow.state}
                paRequest={flow.paRequest}
                error={flow.error}
                onSubmit={() => flow.submit()}
                onCriterionClick={(c) => setSelectedCriterion(c)}
                onViewPdf={() => setPdfOpen(true)}
                onViewConfirmation={() => setConfirmationOpen(true)}
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

      {/* Submission confirmation receipt */}
      <ConfirmationDialog
        isOpen={confirmationOpen}
        onClose={() => setConfirmationOpen(false)}
        paRequest={flow.paRequest}
      />
    </div>
  );
}

export const Route = createFileRoute('/ehr-demo')({
  component: EhrDemoPage,
});
