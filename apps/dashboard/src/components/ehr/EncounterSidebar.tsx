import { useState } from 'react';
import type { EhrDemoState } from './useEhrDemoFlow';
import type { DEMO_CHART_DATA } from '@/lib/demoData';
import { ChartTabPanel } from './ChartTabPanel';

const ENCOUNTER_STAGES = ['Review', 'HPI', 'ROS', 'PE', 'A&P'] as const;

const ENHANCED_ENCOUNTER_STAGES = ['Intake', 'HPI', 'ROS', 'PE', 'A&P', 'Orders', 'Sign'] as const;

const PA_STAGES = ['Analyzing', 'Review', 'Submit', 'Complete'] as const;

const ENHANCED_PA_STAGES = ['PA Review', 'PA Submit'] as const;

export type StageName = typeof ENCOUNTER_STAGES[number] | typeof ENHANCED_ENCOUNTER_STAGES[number];

type ChartData = typeof DEMO_CHART_DATA;

const CHART_TABS = [
  { id: 'problems', label: 'Problems' },
  { id: 'medications', label: 'Meds' },
  { id: 'allergies', label: 'Allergies' },
  { id: 'vitals', label: 'Vitals' },
  { id: 'imaging', label: 'Imaging' },
  { id: 'labs', label: 'Labs' },
] as const;

interface EncounterSidebarProps {
  activeStage?: StageName;
  signed?: boolean;
  flowState?: EhrDemoState;
  preCheckCount?: { met: number; total: number };
  chartData?: ChartData;
  paDetected?: boolean;
}

type StageState = 'completed' | 'active' | 'pending';

function getStageState(
  _stage: string,
  stageIndex: number,
  activeIndex: number,
): StageState {
  if (stageIndex < activeIndex) return 'completed';
  if (stageIndex === activeIndex) return 'active';
  return 'pending';
}

function getPAActiveIndex(flowState: EhrDemoState): number {
  switch (flowState) {
    case 'signing':
    case 'processing':
      return 0;
    case 'reviewing':
      return 1;
    case 'submitting':
      return 2;
    case 'complete':
      return 4; // past all stages — everything completed
    default:
      return -1;
  }
}

function getPAStageState(stageIndex: number, activeIndex: number): StageState {
  if (stageIndex < activeIndex) return 'completed';
  if (stageIndex === activeIndex) return 'active';
  return 'pending';
}

function StageIndicator({ state }: { state: StageState }) {
  if (state === 'completed') {
    return (
      <span className="flex h-5 w-5 items-center justify-center rounded-full bg-green-100 text-green-600">
        <svg
          className="h-3 w-3"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={3}
            d="M5 13l4 4L19 7"
          />
        </svg>
      </span>
    );
  }

  if (state === 'active') {
    return (
      <span className="h-5 w-5 rounded-full bg-teal-500" />
    );
  }

  return (
    <span className="h-3.5 w-3.5 rounded-full border-2 border-gray-300" />
  );
}

function StageList({
  stages,
  getState,
}: {
  stages: readonly string[];
  getState: (stage: string, index: number) => StageState;
}) {
  return (
    <ol className="space-y-1">
      {stages.map((stage, index) => {
        const state = getState(stage, index);
        const isActive = state === 'active';
        const isCompleted = state === 'completed';

        return (
          <li
            key={stage}
            data-stage={stage}
            data-completed={isCompleted ? 'true' : undefined}
            {...(isActive ? { 'aria-current': 'step' as const } : {})}
            className={`flex items-center gap-3 px-4 py-2 ${
              isActive ? 'font-bold text-teal-700' : 'text-gray-400'
            }`}
          >
            <StageIndicator state={state} />
            <span>{stage}</span>
          </li>
        );
      })}
    </ol>
  );
}

function ChartTabsGrid({
  chartData,
  activeTab,
  onTabClick,
}: {
  chartData: ChartData;
  activeTab: string | null;
  onTabClick: (tabId: string) => void;
}) {
  return (
    <div className="px-3 pb-2">
      <h2 className="mb-2 px-1 text-xs font-semibold uppercase tracking-wider text-gray-500">
        Chart
      </h2>
      <div className="grid grid-cols-3 gap-1">
        {CHART_TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => onTabClick(tab.id)}
            className={`rounded px-1.5 py-1 text-[10px] font-medium transition-colors ${
              activeTab === tab.id
                ? 'bg-teal-100 text-teal-700'
                : 'bg-gray-50 text-gray-500 hover:bg-gray-100'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {activeTab && (
        <div className="mt-2 border-t border-gray-100 pt-2">
          <ChartTabPanel activeTab={activeTab} chartData={chartData} />
        </div>
      )}
    </div>
  );
}

export function EncounterSidebar({
  activeStage = 'A&P',
  signed = false,
  flowState = 'idle',
  preCheckCount,
  chartData,
  paDetected,
}: EncounterSidebarProps) {
  const [activeChartTab, setActiveChartTab] = useState<string | null>(null);

  const isEnhanced = !!chartData;
  const stages = isEnhanced ? ENHANCED_ENCOUNTER_STAGES : ENCOUNTER_STAGES;

  // When signed, all encounter stages are completed (activeIndex past last stage)
  const activeIndex = signed ? stages.length : (stages as readonly string[]).indexOf(activeStage);
  const isFlagged = flowState === 'flagged';
  const showPAStages = flowState !== 'idle' && flowState !== 'error' && flowState !== 'flagged';

  // In legacy mode, use PA_STAGES; in enhanced mode, use ENHANCED_PA_STAGES
  const paStages = isEnhanced ? ENHANCED_PA_STAGES : PA_STAGES;
  const paActiveIndex = getPAActiveIndex(flowState);

  const handleTabClick = (tabId: string) => {
    setActiveChartTab((prev) => (prev === tabId ? null : tabId));
  };

  return (
    <aside className="w-[200px] border-r border-gray-200 bg-white py-4">
      {/* Chart tabs section (enhanced mode only) */}
      {isEnhanced && (
        <>
          <ChartTabsGrid
            chartData={chartData}
            activeTab={activeChartTab}
            onTabClick={handleTabClick}
          />
          <div className="my-3 border-t border-gray-200" />
        </>
      )}

      <h2 className="mb-4 px-4 text-xs font-semibold uppercase tracking-wider text-gray-500">
        Encounter
      </h2>
      <nav aria-label="Encounter stages">
        <StageList
          stages={stages}
          getState={(stage, index) => getStageState(stage, index, activeIndex)}
        />
      </nav>

      {/* Enhanced PA stages (when paDetected, before signing) */}
      {isEnhanced && paDetected && !showPAStages && (
        <>
          <div className="my-4 border-t border-gray-200" />
          <h2 className="mb-4 px-4 text-xs font-semibold uppercase tracking-wider text-amber-600">
            Prior Auth
          </h2>
          <nav aria-label="Prior authorization stages">
            <StageList
              stages={ENHANCED_PA_STAGES}
              getState={() => 'pending'}
            />
          </nav>
        </>
      )}

      {isFlagged && preCheckCount && (
        <>
          <div className="my-4 border-t border-gray-200" />
          <h2 className="mb-4 px-4 text-xs font-semibold uppercase tracking-wider text-amber-600">
            Policy Check
          </h2>
          <div className="flex items-center gap-2 px-4 py-1 text-sm text-amber-700">
            <span className="h-2 w-2 rounded-full bg-amber-500" />
            {preCheckCount.met}/{preCheckCount.total} documented
          </div>
        </>
      )}

      {showPAStages && (
        <>
          <div className="my-4 border-t border-gray-200" />
          <h2 className="mb-4 px-4 text-xs font-semibold uppercase tracking-wider text-blue-600">
            Prior Auth
          </h2>
          <nav aria-label="Prior authorization stages">
            <StageList
              stages={paStages}
              getState={(_stage, index) => getPAStageState(index, paActiveIndex)}
            />
          </nav>
        </>
      )}
    </aside>
  );
}
