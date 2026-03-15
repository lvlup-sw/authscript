import { describe, it, expect, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { createElement, useState, type ReactNode } from 'react';
import { SceneTransition } from '../SceneTransition';

// Mock motion to avoid animation issues in jsdom
vi.mock('motion/react', () => ({
  AnimatePresence: ({ children }: { children: ReactNode }) =>
    createElement('div', { 'data-testid': 'animate-presence' }, children),
  motion: {
    div: ({ children }: { children?: ReactNode }) =>
      createElement('div', { 'data-testid': 'motion-div' }, children),
  },
}));

function SceneController({ initialScene = 'encounter' }: { initialScene?: string }) {
  const [sceneKey, setSceneKey] = useState(initialScene);

  const sceneContent =
    sceneKey === 'encounter'
      ? createElement('div', { 'data-testid': 'encounter-content' }, 'Encounter Scene')
      : sceneKey === 'fleet'
        ? createElement('div', { 'data-testid': 'fleet-content' }, 'Fleet Scene')
        : createElement('div', { 'data-testid': 'case-content' }, 'Case Scene');

  return createElement(
    'div',
    null,
    createElement('button', { 'data-testid': 'set-fleet', onClick: () => setSceneKey('fleet') }, 'Go Fleet'),
    createElement('button', { 'data-testid': 'set-case', onClick: () => setSceneKey('case') }, 'Go Case'),
    createElement('button', { 'data-testid': 'set-encounter', onClick: () => setSceneKey('encounter') }, 'Go Encounter'),
    createElement(SceneTransition, { sceneKey, children: sceneContent }),
  );
}

describe('SceneTransition', () => {
  it('SceneTransition_Default_RendersEncounterContent', () => {
    render(createElement(SceneController));
    expect(screen.getByTestId('encounter-content')).toBeInTheDocument();
  });

  it('SceneTransition_EncounterToFleet_RendersFleetScene', () => {
    render(createElement(SceneController));
    expect(screen.getByTestId('encounter-content')).toBeInTheDocument();

    act(() => {
      screen.getByTestId('set-fleet').click();
    });

    expect(screen.getByTestId('fleet-content')).toBeInTheDocument();
    expect(screen.queryByTestId('encounter-content')).not.toBeInTheDocument();
  });

  it('SceneTransition_FleetToCase_RendersCaseScene', () => {
    render(createElement(SceneController, { initialScene: 'fleet' }));
    expect(screen.getByTestId('fleet-content')).toBeInTheDocument();

    act(() => {
      screen.getByTestId('set-case').click();
    });

    expect(screen.getByTestId('case-content')).toBeInTheDocument();
    expect(screen.queryByTestId('fleet-content')).not.toBeInTheDocument();
  });

  it('SceneTransition_PillNav_AllowsNonLinearNavigation', () => {
    render(createElement(SceneController));
    expect(screen.getByTestId('encounter-content')).toBeInTheDocument();

    // Jump directly from encounter to case (skipping fleet)
    act(() => {
      screen.getByTestId('set-case').click();
    });

    expect(screen.getByTestId('case-content')).toBeInTheDocument();
    expect(screen.queryByTestId('encounter-content')).not.toBeInTheDocument();
    expect(screen.queryByTestId('fleet-content')).not.toBeInTheDocument();
  });

  it('SceneTransition_AcceptsDirectionProp', () => {
    // Verify component renders without error when direction prop is provided
    render(
      createElement(SceneTransition, {
        sceneKey: 'encounter',
        direction: 'zoom-out',
        children: createElement('div', null, 'Test content'),
      }),
    );
    expect(screen.getByText('Test content')).toBeInTheDocument();
  });
});
