import type { EhrDemoState } from './useEhrDemoFlow';

const ENCOUNTER_STAGES = ['Review', 'HPI', 'ROS', 'PE', 'A&P'] as const;

const PA_STAGES = ['Analyzing', 'Review', 'Submit', 'Complete'] as const;

export type StageName = typeof ENCOUNTER_STAGES[number];

interface EncounterSidebarProps {
  activeStage?: StageName;
  signed?: boolean;
  flowState?: EhrDemoState;
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

export function EncounterSidebar({
  activeStage = 'A&P',
  signed = false,
  flowState = 'idle',
}: EncounterSidebarProps) {
  // When signed, all encounter stages are completed (activeIndex past last stage)
  const activeIndex = signed ? ENCOUNTER_STAGES.length : ENCOUNTER_STAGES.indexOf(activeStage);
  const showPA = flowState !== 'idle' && flowState !== 'error';
  const paActiveIndex = getPAActiveIndex(flowState);

  return (
    <aside className="w-[200px] border-r border-gray-200 bg-white py-4">
      <h2 className="mb-4 px-4 text-xs font-semibold uppercase tracking-wider text-gray-500">
        Encounter
      </h2>
      <nav aria-label="Encounter stages">
        <StageList
          stages={ENCOUNTER_STAGES}
          getState={(stage, index) => getStageState(stage, index, activeIndex)}
        />
      </nav>

      {showPA && (
        <>
          <div className="my-4 border-t border-gray-200" />
          <h2 className="mb-4 px-4 text-xs font-semibold uppercase tracking-wider text-blue-600">
            Prior Auth
          </h2>
          <nav aria-label="Prior authorization stages">
            <StageList
              stages={PA_STAGES}
              getState={(_stage, index) => getPAStageState(index, paActiveIndex)}
            />
          </nav>
        </>
      )}
    </aside>
  );
}
