import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import type { ReactNode } from 'react';
import { DemoProvider } from '../DemoProvider';
import { SceneNav } from '../SceneNav';

function wrapper({ children }: { children: ReactNode }) {
  return <DemoProvider>{children}</DemoProvider>;
}

function renderSceneNav() {
  return render(<SceneNav />, { wrapper });
}

describe('SceneNav', () => {
  it('SceneNav_RendersThreePills_EncounterFleetCase', () => {
    renderSceneNav();

    expect(screen.getByRole('button', { name: /encounter/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /fleet/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /case detail/i })).toBeInTheDocument();
  });

  it('SceneNav_ActiveScene_HasFilledStyle', () => {
    renderSceneNav();

    // Default scene is 'encounter', so it should have the active data attribute
    const encounterBtn = screen.getByRole('button', { name: /encounter/i });
    expect(encounterBtn).toHaveAttribute('data-active', 'true');

    const fleetBtn = screen.getByRole('button', { name: /fleet/i });
    expect(fleetBtn).toHaveAttribute('data-active', 'false');
  });

  it('SceneNav_ClickPill_CallsSetScene', () => {
    renderSceneNav();

    const fleetBtn = screen.getByRole('button', { name: /fleet/i });
    fireEvent.click(fleetBtn);

    // After click, fleet should now be active
    expect(fleetBtn).toHaveAttribute('data-active', 'true');

    // And encounter should be inactive
    const encounterBtn = screen.getByRole('button', { name: /encounter/i });
    expect(encounterBtn).toHaveAttribute('data-active', 'false');
  });

  it('SceneNav_DemoControls_RendersResetButton', () => {
    renderSceneNav();

    expect(screen.getByRole('button', { name: /reset demo/i })).toBeInTheDocument();
  });
});
