import { Handle, Position } from '@xyflow/react';
import { NodeCard } from './NodeCard';
import type { NodeProps } from '@xyflow/react';

export interface CriteriaNodeData {
  label: string;
  status: 'met' | 'not_met' | 'indeterminate';
  reasoning?: string;
}

const STATUS_CONFIG = {
  met: {
    border: 'border-green-400',
    icon: '\u2713',
    iconBg: 'bg-green-500 text-white',
    testId: 'criteria-icon-met',
  },
  not_met: {
    border: 'border-red-400',
    icon: '\u2717',
    iconBg: 'bg-red-500 text-white',
    testId: 'criteria-icon-not_met',
  },
  indeterminate: {
    border: 'border-amber-400',
    icon: '?',
    iconBg: 'bg-amber-500 text-white',
    testId: 'criteria-icon-indeterminate',
  },
} as const;

/**
 * Custom React Flow node displaying a policy criterion and its status.
 * Layout: status icon + criterion label. Border colored by status.
 */
export function CriteriaNode({
  data,
}: NodeProps & { data: CriteriaNodeData }) {
  const config = STATUS_CONFIG[data.status];

  return (
    <NodeCard borderColor={config.border} className="max-w-[260px]">
      <Handle
        type="target"
        position={Position.Top}
        className="!bg-slate-400"
      />

      <div className="flex items-center gap-2">
        <div
          data-testid={config.testId}
          className={`w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0 text-xs font-bold ${config.iconBg}`}
        >
          {config.icon}
        </div>
        <span className="text-xs font-medium text-gray-800 leading-tight">
          {data.label}
        </span>
      </div>

      <Handle
        type="source"
        position={Position.Bottom}
        className="!bg-slate-400"
      />
    </NodeCard>
  );
}
