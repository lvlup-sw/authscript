import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import { ChevronDown, ChevronRight, User, Clock, Shield, Sparkles } from 'lucide-react';
import { CaseGraph } from '@/components/case/CaseGraph';
import { CaseTimeline } from '@/components/case/CaseTimeline';
import type { TimelinePhase } from '@/components/case/CaseTimeline';
import { DEMO_PA_RESULT, LCD_L34220_POLICY, DEMO_PA_RESULT_SOURCES } from '@/lib/demoData';
import type { PARequest } from '@/api/graphqlService';
import '@xyflow/react/dist/style.css';

export const Route = createFileRoute('/case/$caseId')({
  component: CaseDetailPage,
});

/** Scene navigation tabs for the demo */
const SCENES = [
  { id: 'ehr', label: 'EHR Demo' },
  { id: 'case', label: 'Case Detail' },
  { id: 'dashboard', label: 'Dashboard' },
];

function SceneNav() {
  return (
    <nav data-testid="scene-nav" className="flex items-center gap-1 p-1 bg-slate-100 rounded-lg w-fit">
      {SCENES.map((scene) => (
        <button
          key={scene.id}
          className={cn(
            'px-4 py-1.5 rounded-md text-sm font-medium transition-colors',
            scene.id === 'case'
              ? 'bg-white text-slate-900 shadow-sm'
              : 'text-slate-500 hover:text-slate-700',
          )}
        >
          {scene.label}
        </button>
      ))}
    </nav>
  );
}

/** Timeline phases derived from PA request timestamps */
function buildTimelinePhases(pa: PARequest): TimelinePhase[] {
  const phases: TimelinePhase[] = [];

  const createdTime = pa.createdAt
    ? new Date(pa.createdAt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
    : undefined;
  const readyTime = pa.readyAt
    ? new Date(pa.readyAt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
    : undefined;

  // Submitted phase
  phases.push({
    name: 'Submitted',
    status: pa.createdAt ? 'completed' : 'pending',
    timestamp: createdTime,
    duration: '0.8s',
  });

  // Analyzing phase
  phases.push({
    name: 'Analyzing',
    status: pa.readyAt ? 'completed' : pa.createdAt ? 'active' : 'pending',
    timestamp: createdTime,
    duration: pa.readyAt ? '4.2s' : undefined,
  });

  // Review phase
  const isReview = pa.status === 'ready' && pa.readyAt;
  const isPastReview = pa.status === 'waiting_for_insurance' || pa.status === 'approved' || pa.status === 'denied';
  phases.push({
    name: 'Review Ready',
    status: isPastReview ? 'completed' : isReview ? 'active' : 'pending',
    timestamp: readyTime,
  });

  // Payer submission phase
  const isSubmitted = pa.submittedAt != null;
  phases.push({
    name: 'Payer Submission',
    status: isSubmitted ? 'completed' : isPastReview ? 'active' : 'pending',
    timestamp: pa.submittedAt
      ? new Date(pa.submittedAt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
      : undefined,
  });

  // Decision phase
  phases.push({
    name: 'Decision',
    status: pa.status === 'approved' || pa.status === 'denied' ? 'completed' : 'pending',
  });

  return phases;
}

/** Expandable criteria section */
function CriteriaSection({ pa }: { pa: PARequest }) {
  const [expanded, setExpanded] = useState<Record<number, boolean>>({});

  return (
    <div className="space-y-2">
      {pa.criteria.map((c, i) => {
        const isOpen = expanded[i] ?? false;
        const sourceInfo = DEMO_PA_RESULT_SOURCES[c.label];
        const statusColor = c.met === true ? 'border-green-300 bg-green-50/50' : c.met === false ? 'border-red-300 bg-red-50/50' : 'border-amber-300 bg-amber-50/50';
        const iconColor = c.met === true ? 'text-green-600' : c.met === false ? 'text-red-600' : 'text-amber-600';

        return (
          <div key={i} className={cn('rounded-lg border', statusColor)}>
            <button
              onClick={() => setExpanded((prev) => ({ ...prev, [i]: !isOpen }))}
              className="w-full flex items-center gap-2 p-3 text-left"
            >
              <span className={cn('text-sm font-bold', iconColor)}>
                {c.met === true ? '\u2713' : c.met === false ? '\u2717' : '?'}
              </span>
              <span className="flex-1 text-sm font-medium text-gray-800">{c.label}</span>
              {isOpen ? (
                <ChevronDown className="w-4 h-4 text-gray-400" />
              ) : (
                <ChevronRight className="w-4 h-4 text-gray-400" />
              )}
            </button>
            {isOpen && (
              <div className="px-3 pb-3 space-y-2">
                {c.reason && (
                  <p className="text-xs text-gray-600 leading-relaxed">{c.reason}</p>
                )}
                {sourceInfo && (
                  <div className="flex items-center gap-2 text-[10px]">
                    <span className="px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 font-medium">
                      {sourceInfo.source}
                    </span>
                    <span className="text-gray-500">{sourceInfo.evidence}</span>
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

/** Main case detail page component */
export function CaseDetailPage() {
  // Use demo data (in a real app, this would come from context or API)
  const paRequest = DEMO_PA_RESULT;
  const phases = buildTimelinePhases(paRequest);

  const metCount = paRequest.criteria.filter((c) => c.met === true).length;
  const totalCount = paRequest.criteria.length;

  return (
    <div className="p-6 space-y-4 animate-fade-in">
      {/* Scene navigation */}
      <SceneNav />

      {/* Split layout */}
      <div className="flex gap-6" style={{ minHeight: 'calc(100vh - 180px)' }}>
        {/* Left panel: Case Graph (55%) */}
        <div className="w-[55%] flex-shrink-0">
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden h-full">
            <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
              <h2 className="text-sm font-bold text-gray-800">Evidence Graph</h2>
              <span className="text-[10px] text-gray-400 font-mono">
                {LCD_L34220_POLICY.policyId}
              </span>
            </div>
            <div className="h-[calc(100%-48px)]">
              <CaseGraph paRequest={paRequest} />
            </div>
          </div>
        </div>

        {/* Right panel: Detail cards (45%) */}
        <div className="flex-1 space-y-4 overflow-y-auto">
          {/* Timeline card */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-sm font-bold text-gray-800 flex items-center gap-2 mb-4">
              <Clock className="w-4 h-4 text-teal" />
              Timeline
            </h3>
            <CaseTimeline phases={phases} />
          </div>

          {/* Patient summary card */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-sm font-bold text-gray-800 flex items-center gap-2 mb-3">
              <User className="w-4 h-4 text-teal" />
              Patient Summary
            </h3>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <p className="text-[10px] text-gray-400 uppercase tracking-wide">Name</p>
                <p className="text-sm font-medium text-gray-900">{paRequest.patient.name}</p>
              </div>
              <div>
                <p className="text-[10px] text-gray-400 uppercase tracking-wide">DOB</p>
                <p className="text-sm font-medium text-gray-900">{paRequest.patient.dob}</p>
              </div>
              <div>
                <p className="text-[10px] text-gray-400 uppercase tracking-wide">MRN</p>
                <p className="text-sm font-mono font-medium text-gray-900">{paRequest.patient.mrn}</p>
              </div>
              <div>
                <p className="text-[10px] text-gray-400 uppercase tracking-wide">Insurance</p>
                <p className="text-sm font-medium text-gray-900">{paRequest.payer}</p>
              </div>
              <div className="col-span-2">
                <p className="text-[10px] text-gray-400 uppercase tracking-wide">Provider</p>
                <p className="text-sm font-medium text-gray-900">{paRequest.provider}</p>
              </div>
            </div>
          </div>

          {/* Criteria Evidence card */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-bold text-gray-800 flex items-center gap-2">
                <Shield className="w-4 h-4 text-teal" />
                Criteria Evidence
              </h3>
              <span className="px-2 py-0.5 rounded-full bg-green-50 text-green-700 text-[10px] font-bold">
                {metCount}/{totalCount} met
              </span>
            </div>
            <CriteriaSection pa={paRequest} />
          </div>

          {/* Clinical Summary card */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-sm font-bold text-gray-800 flex items-center gap-2 mb-3">
              <Sparkles className="w-4 h-4 text-teal" />
              Clinical Summary
            </h3>
            <div className="p-3 rounded-lg bg-slate-50 border border-slate-100">
              <p className="text-xs text-gray-700 leading-relaxed">
                {paRequest.clinicalSummary}
              </p>
            </div>
            <div className="mt-2 flex items-center gap-2">
              <span className="text-[10px] text-gray-400">Confidence:</span>
              <span className={cn(
                'text-sm font-bold',
                paRequest.confidence >= 80 ? 'text-green-600' : paRequest.confidence >= 60 ? 'text-amber-600' : 'text-red-600',
              )}>
                {paRequest.confidence}%
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
