import { Handle, Position } from '@xyflow/react';
import { NodeCard } from './NodeCard';
import type { NodeProps } from '@xyflow/react';

export interface EvidenceNodeData {
  text: string;
  source: string;
}

const SOURCE_COLORS: Record<string, string> = {
  HPI: 'bg-blue-100 text-blue-700',
  Assessment: 'bg-purple-100 text-purple-700',
  Orders: 'bg-amber-100 text-amber-700',
  'Imaging History': 'bg-slate-100 text-slate-700',
  'Problem List': 'bg-green-100 text-green-700',
  'Assessment / Plan': 'bg-indigo-100 text-indigo-700',
  'CC / HPI': 'bg-sky-100 text-sky-700',
  'HPI / Orders': 'bg-cyan-100 text-cyan-700',
};

/**
 * Custom React Flow node displaying a piece of clinical evidence.
 * Layout: evidence text + small source badge (colored by type).
 */
export function EvidenceNode({ data }: NodeProps & { data: EvidenceNodeData }) {
  const sourceColor =
    SOURCE_COLORS[data.source] ?? 'bg-slate-100 text-slate-600';

  return (
    <NodeCard borderColor="border-blue-200" className="max-w-[240px]">
      <Handle type="target" position={Position.Top} className="!bg-blue-400" />

      <p className="text-xs text-gray-700 leading-relaxed">{data.text}</p>

      <div className="mt-2 flex justify-end">
        <span
          className={`px-2 py-0.5 rounded-full text-[10px] font-semibold ${sourceColor}`}
        >
          {data.source}
        </span>
      </div>

      <Handle
        type="source"
        position={Position.Bottom}
        className="!bg-blue-400"
      />
    </NodeCard>
  );
}
