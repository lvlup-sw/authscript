import { describe, it, expect, vi } from 'vitest';
import { render } from '@testing-library/react';

// Mock @xyflow/react utilities
vi.mock('@xyflow/react', () => ({
  getSmoothStepPath: () => ['M 0 0 L 100 100', 0, 0],
  BaseEdge: ({ path }: { path: string }) => <path d={path} data-testid="base-edge-path" />,
  Position: { Top: 'top', Bottom: 'bottom', Left: 'left', Right: 'right' },
}));

import { AnimatedEdge } from '../AnimatedEdge';

describe('AnimatedEdge', () => {
  const baseEdgeProps = {
    id: 'edge-1',
    source: 'node-1',
    target: 'node-2',
    sourceX: 0,
    sourceY: 0,
    targetX: 100,
    targetY: 100,
    sourcePosition: 'bottom' as const,
    targetPosition: 'top' as const,
    style: {},
    markerEnd: undefined,
    data: {},
  };

  it('AnimatedEdge_RendersBaseSVGPath', () => {
    const { container } = render(
      <svg>
        <AnimatedEdge {...(baseEdgeProps as any)} />
      </svg>,
    );
    const path = container.querySelector('[data-testid="base-edge-path"]');
    expect(path).not.toBeNull();
  });
});
