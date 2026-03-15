import { getSmoothStepPath, BaseEdge, type EdgeProps } from '@xyflow/react';

export interface AnimatedEdgeData {
  animationDelay?: number;
}

/**
 * Custom React Flow edge with an animated dot traveling along the path.
 * Uses SVG <circle> with <animateMotion> for the animation effect.
 */
export function AnimatedEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  style = {},
  markerEnd,
  data,
}: EdgeProps & { data?: AnimatedEdgeData }) {
  const [edgePath] = getSmoothStepPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  const delay = data?.animationDelay ?? 0;

  return (
    <>
      <BaseEdge
        id={id}
        path={edgePath}
        markerEnd={markerEnd}
        style={{
          ...style,
          strokeDasharray: '6 4',
          strokeWidth: 1.5,
          stroke: '#94a3b8',
        }}
      />
      <circle r="3" fill="#0d9488" opacity="0.8">
        <animateMotion
          dur="2.5s"
          repeatCount="indefinite"
          path={edgePath}
          begin={`${delay}s`}
        />
      </circle>
    </>
  );
}
