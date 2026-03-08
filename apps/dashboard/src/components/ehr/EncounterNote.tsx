import { useRef, useEffect, useState } from 'react';
import { Sparkles, Check, Loader2, CloudUpload } from 'lucide-react';
import type { DocState } from './useEhrDemoFlow';

export interface Encounter {
  cc: string;
  hpi: string;
  assessment: string;
  plan: string;
}

export interface Vitals {
  bp: string;
  hr: number;
  temp: number;
  spo2: number;
}

export interface Order {
  code: string;
  name: string;
  status: 'requires-pa' | 'pending' | 'completed';
}

interface EncounterNoteProps {
  encounter: Encounter;
  vitals?: Vitals;
  orders?: Order[];
  hpiHighlight?: string;
  assessmentHighlight?: string;
  /** Documentation sub-flow state */
  docState?: DocState;
  /** The suggested text to show in the AI suggestion box */
  suggestionText?: string;
  /** Called when user clicks "Add to Note" — receives the (possibly edited) text */
  onInsertSuggestion?: (editedText: string) => void;
  /** Called when user clicks "Save to Chart" after inserting */
  onSaveToChart?: () => void;
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <h3 className="mb-1 text-xs font-bold uppercase tracking-wide text-gray-500">
      {children}
    </h3>
  );
}

function EncounterSection({
  label,
  text,
  highlight,
}: {
  label: string;
  text: string;
  highlight?: string;
}) {
  if (highlight && text.includes(highlight)) {
    const idx = text.indexOf(highlight);
    const before = text.slice(0, idx);
    const after = text.slice(idx + highlight.length);
    return (
      <div>
        <SectionLabel>{label}</SectionLabel>
        <p className="text-sm text-gray-700">
          {before}
          <mark className="rounded bg-green-100 px-0.5 text-green-900 animate-fadeIn">
            {highlight}
          </mark>
          {after}
        </p>
      </div>
    );
  }
  return (
    <div>
      <SectionLabel>{label}</SectionLabel>
      <p className="text-sm text-gray-700">{text}</p>
    </div>
  );
}

function VitalsRow({ vitals }: { vitals: Vitals }) {
  return (
    <div className="flex gap-6 border-b border-gray-100 px-5 py-3">
      <div className="text-sm">
        <span className="font-medium text-gray-500">BP</span>{' '}
        <span className="text-gray-700">{vitals.bp}</span>
      </div>
      <div className="text-sm">
        <span className="font-medium text-gray-500">HR</span>{' '}
        <span className="text-gray-700">{vitals.hr}</span>
      </div>
      <div className="text-sm">
        <span className="font-medium text-gray-500">Temp</span>{' '}
        <span className="text-gray-700">{vitals.temp}&deg;F</span>
      </div>
      <div className="text-sm">
        <span className="font-medium text-gray-500">SpO2</span>{' '}
        <span className="text-gray-700">{vitals.spo2}%</span>
      </div>
    </div>
  );
}

const STATUS_CONFIG: Record<Order['status'], { label: string; colors: string }> = {
  'requires-pa': { label: 'Requires PA', colors: 'bg-amber-100 text-amber-800' },
  pending: { label: 'Pending', colors: 'bg-gray-100 text-gray-700' },
  completed: { label: 'Completed', colors: 'bg-green-100 text-green-800' },
};

function StatusBadge({ status }: { status: Order['status'] }) {
  const config = STATUS_CONFIG[status];
  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${config.colors}`}>
      {config.label}
    </span>
  );
}

function OrdersCard({ orders }: { orders: Order[] }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white shadow-sm">
      <div className="border-b border-gray-100 px-5 py-3">
        <h2 className="text-sm font-semibold text-gray-800">Orders</h2>
      </div>
      <div className="divide-y divide-gray-100 px-5">
        {orders.map((order) => (
          <div key={order.code} className="flex items-center gap-3 py-3">
            <span className="inline-flex items-center rounded bg-gray-100 px-2 py-0.5 text-xs font-mono text-gray-600">
              {order.code}
            </span>
            <span className="text-sm text-gray-700">{order.name}</span>
            <StatusBadge status={order.status} />
          </div>
        ))}
      </div>
    </div>
  );
}

/** AI Suggestion box shown inside the HPI section during the documentation flow */
function AISuggestionBox({
  text,
  onInsert,
}: {
  text: string;
  onInsert: (editedText: string) => void;
}) {
  const [value, setValue] = useState(text);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-focus and select all text on mount so it feels interactive
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.focus();
      textareaRef.current.select();
    }
  }, []);

  return (
    <div className="mt-3 rounded-lg border border-blue-200 bg-blue-50/50 p-3 animate-fadeIn">
      <div className="flex items-center gap-1.5 mb-2">
        <Sparkles className="h-3.5 w-3.5 text-blue-600" />
        <span className="text-xs font-semibold uppercase tracking-wider text-blue-600">
          AuthScript Suggestion
        </span>
        <span className="text-[10px] text-slate-400 ml-auto">
          Edit before adding
        </span>
      </div>
      <textarea
        ref={textareaRef}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        rows={3}
        className="w-full resize-none rounded border border-blue-100 bg-white p-2.5 text-sm text-gray-700 font-mono leading-relaxed focus:border-blue-300 focus:outline-none focus:ring-1 focus:ring-blue-200"
      />
      <button
        type="button"
        onClick={() => onInsert(value)}
        disabled={!value.trim()}
        className="mt-2.5 inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3.5 py-1.5 text-xs font-medium text-white shadow-sm transition-colors hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <Check className="h-3.5 w-3.5" />
        Add to Note
      </button>
    </div>
  );
}

/** Save-to-chart status bar shown after text is inserted */
function SaveToChartBar({
  docState,
  onSave,
}: {
  docState: DocState;
  onSave: () => void;
}) {
  if (docState === 'inserted') {
    return (
      <div className="flex items-center justify-between rounded-lg border border-green-200 bg-green-50 px-4 py-2.5 animate-fadeIn">
        <span className="text-sm text-green-800">
          Documentation updated. Save changes to the patient chart.
        </span>
        <button
          type="button"
          onClick={onSave}
          className="inline-flex items-center gap-1.5 rounded-md bg-green-700 px-3.5 py-1.5 text-xs font-medium text-white shadow-sm transition-colors hover:bg-green-800"
        >
          <CloudUpload className="h-3.5 w-3.5" />
          Save to Chart
        </button>
      </div>
    );
  }

  if (docState === 'saving') {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5 animate-fadeIn">
        <Loader2 className="h-4 w-4 animate-spin text-slate-500" />
        <span className="text-sm text-slate-600">
          Saving to athenahealth...
        </span>
      </div>
    );
  }

  if (docState === 'saved') {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 px-4 py-2.5 animate-fadeIn">
        <div className="flex h-5 w-5 items-center justify-center rounded-full bg-green-600">
          <Check className="h-3 w-3 text-white" />
        </div>
        <span className="text-sm font-medium text-green-800">
          Saved to chart
        </span>
        <span className="text-xs text-green-600">
          &mdash; encounter note updated via athenahealth API
        </span>
      </div>
    );
  }

  return null;
}

export function EncounterNote({
  encounter,
  vitals,
  orders,
  hpiHighlight,
  assessmentHighlight,
  docState = 'idle',
  suggestionText,
  onInsertSuggestion,
  onSaveToChart,
}: EncounterNoteProps) {
  const enhanced = !!(vitals || orders);
  const hpiDetailsRef = useRef<HTMLDetailsElement>(null);

  // Auto-open CC/HPI section when suggestion is triggered
  useEffect(() => {
    if (docState === 'suggesting' && hpiDetailsRef.current) {
      hpiDetailsRef.current.open = true;
    }
  }, [docState]);

  const showSuggestion = docState === 'suggesting' && suggestionText && onInsertSuggestion;
  const showSaveBar = docState === 'inserted' || docState === 'saving' || docState === 'saved';

  return (
    <div className={enhanced ? 'space-y-4' : ''}>
      <div className="rounded-lg border border-gray-200 bg-white shadow-sm">
        <div className="border-b border-gray-100 px-5 py-3">
          <h2 className="text-sm font-semibold text-gray-800">
            Encounter Note
          </h2>
        </div>

        {vitals && <VitalsRow vitals={vitals} />}

        <div className="divide-y divide-gray-100 px-5">
          {enhanced ? (
            <>
              {/* CC/HPI — collapsed by default, auto-opens during suggestion flow */}
              <details ref={hpiDetailsRef} className="py-3">
                <summary className="cursor-pointer text-xs font-bold uppercase tracking-wide text-gray-500">
                  Chief Complaint / HPI
                </summary>
                <div className="mt-2 space-y-3">
                  <EncounterSection label="Chief Complaint" text={encounter.cc} />
                  <EncounterSection label="History of Present Illness" text={encounter.hpi} highlight={hpiHighlight} />

                  {/* AI suggestion box — visible during 'suggesting' state */}
                  {showSuggestion && (
                    <AISuggestionBox
                      text={suggestionText}
                      onInsert={onInsertSuggestion}
                    />
                  )}
                </div>
              </details>

              {/* Assessment & Plan — expanded by default */}
              <details open className="py-3">
                <summary className="cursor-pointer text-xs font-bold uppercase tracking-wide text-gray-500">
                  Assessment &amp; Plan
                </summary>
                <div className="mt-2 space-y-3">
                  <EncounterSection label="Assessment" text={encounter.assessment} highlight={assessmentHighlight} />
                  <EncounterSection label="Plan" text={encounter.plan} />
                </div>
              </details>
            </>
          ) : (
            <>
              <div className="py-3">
                <EncounterSection label="Chief Complaint" text={encounter.cc} />
              </div>
              <div className="py-3">
                <EncounterSection label="History of Present Illness" text={encounter.hpi} />
              </div>
              <div className="py-3">
                <EncounterSection label="Assessment" text={encounter.assessment} />
              </div>
              <div className="py-3">
                <EncounterSection label="Plan" text={encounter.plan} />
              </div>
            </>
          )}
        </div>
      </div>

      {orders && orders.length > 0 && <OrdersCard orders={orders} />}

      {/* Save-to-chart bar — appears after inserting text */}
      {showSaveBar && onSaveToChart && (
        <SaveToChartBar docState={docState} onSave={onSaveToChart} />
      )}
    </div>
  );
}
