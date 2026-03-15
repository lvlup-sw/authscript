import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SceneTransition } from '../SceneTransition';

// Mock motion/react to avoid animation complexity in tests
vi.mock('motion/react', () => {
  const React = require('react');

  const AnimatePresence = ({ children }: { children: React.ReactNode }) => (
    <div data-testid="animate-presence">{children}</div>
  );

  const motionDiv = React.forwardRef(
    (
      {
        children,
        ...props
      }: { children?: React.ReactNode; [key: string]: unknown },
      ref: React.Ref<HTMLDivElement>,
    ) => (
      <div ref={ref} data-testid="motion-div" {...props}>
        {children}
      </div>
    ),
  );
  motionDiv.displayName = 'motion.div';

  return {
    AnimatePresence,
    motion: { div: motionDiv },
  };
});

describe('SceneTransition', () => {
  it('SceneTransition_RendersActiveScene_WithChildren', () => {
    render(
      <SceneTransition sceneKey="encounter">
        <div>Encounter content</div>
      </SceneTransition>,
    );

    expect(screen.getByText('Encounter content')).toBeInTheDocument();
  });

  it('SceneTransition_SceneChange_AnimatesTransition', () => {
    const { rerender } = render(
      <SceneTransition sceneKey="encounter">
        <div>Scene A</div>
      </SceneTransition>,
    );

    // AnimatePresence should be rendered wrapping the content
    expect(screen.getByTestId('animate-presence')).toBeInTheDocument();
    expect(screen.getByTestId('motion-div')).toBeInTheDocument();

    // Re-render with new scene key
    rerender(
      <SceneTransition sceneKey="fleet">
        <div>Scene B</div>
      </SceneTransition>,
    );

    // New content should render
    expect(screen.getByText('Scene B')).toBeInTheDocument();
    // AnimatePresence should still be wrapping
    expect(screen.getByTestId('animate-presence')).toBeInTheDocument();
  });
});
