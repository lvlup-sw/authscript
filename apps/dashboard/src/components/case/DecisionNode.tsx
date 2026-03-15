import { Handle, Position } from '@xyflow/react';
import { NodeCard } from './NodeCard';
import type { NodeProps } from '@xyflow/react';

export interface DecisionNodeData {
  payer: string;
  policyId: string;
  confidence: number;
  status: string;
}

/**
 * Custom React Flow node displaying the PA decision outcome.
 * Layout: payer + policy ref + confidence % with animated SVG ring + status.
 */
export function DecisionNode({
  data,
}: NodeProps & { data: DecisionNodeData }) {
  const size = 64;
  const strokeWidth = 5;
  const radius = (size - strokeWidth) / 2;
  const circumference = radius * 2 * Math.PI;
  const offset = circumference - (data.confidence / 100) * circumference;

  const colorClass =
    data.confidence >= 80
      ? 'text-green-500'
      : data.confidence >= 60
        ? 'text-amber-500'
        : 'text-red-500';

  return (
    <NodeCard borderColor="border-teal" className="min-w-[200px]">
      <Handle type="target" position={Position.Top} className="!bg-teal" />

      <div className="flex items-center gap-3">
        {/* Animated confidence ring */}
        <div className="relative flex-shrink-0" style={{ width: size, height: size }}>
          <svg
            className="transform -rotate-90"
            width={size}
            height={size}
          >
            <circle
              className="text-gray-200"
              strokeWidth={strokeWidth}
              stroke="currentColor"
              fill="transparent"
              r={radius}
              cx={size / 2}
              cy={size / 2}
            />
            <circle
              className={colorClass}
              strokeWidth={strokeWidth}
              strokeDasharray={circumference}
              strokeDashoffset={offset}
              strokeLinecap="round"
              stroke="currentColor"
              fill="transparent"
              r={radius}
              cx={size / 2}
              cy={size / 2}
              style={{
                transition: 'stroke-dashoffset 1s ease-out',
              }}
            />
          </svg>
          <div className="absolute inset-0 flex items-center justify-center">
            <span className={`text-sm font-bold ${colorClass}`}>
              {data.confidence}%
            </span>
          </div>
        </div>

        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-gray-900">{data.payer}</p>
          <p className="text-[10px] text-gray-500 font-mono">{data.policyId}</p>
          <span className="mt-1 inline-block px-2 py-0.5 rounded-full bg-teal/10 text-teal text-[10px] font-semibold capitalize">
            {data.status}
          </span>
        </div>
      </div>
    </NodeCard>
  );
}
