import { Handle, Position } from '@xyflow/react';
import { NodeCard } from './NodeCard';
import { getInitials } from '@/lib/formatUtils';
import type { NodeProps } from '@xyflow/react';

export interface PatientNodeData {
  name: string;
  dob: string;
  mrn: string;
  insurance: string;
}

/**
 * Custom React Flow node displaying patient demographics.
 * Layout: initials avatar (teal ring) + name + DOB + MRN + insurance badge.
 */
export function PatientNode({ data }: NodeProps & { data: PatientNodeData }) {
  const initials = getInitials(data.name);

  return (
    <NodeCard borderColor="border-teal" className="min-w-[220px]">
      <div className="flex items-center gap-3">
        {/* Initials avatar */}
        <div className="w-10 h-10 rounded-full border-2 border-teal bg-teal/10 flex items-center justify-center flex-shrink-0">
          <span className="text-sm font-bold text-teal">{initials}</span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-sm text-gray-900 truncate">
            {data.name}
          </p>
          <p className="text-xs text-gray-500">DOB: {data.dob}</p>
        </div>
      </div>

      <div className="mt-2 flex items-center justify-between text-xs">
        <span className="text-gray-500">
          MRN: <span className="font-mono font-medium text-gray-700">{data.mrn}</span>
        </span>
        <span className="px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 font-medium">
          {data.insurance}
        </span>
      </div>

      <Handle type="source" position={Position.Bottom} className="!bg-teal" />
    </NodeCard>
  );
}
