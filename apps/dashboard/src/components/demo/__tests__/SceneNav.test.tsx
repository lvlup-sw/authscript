import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import type { ReactNode } from 'react';
import { DemoProvider } from '../DemoProvider';
import { SceneNav } from '../SceneNav';

// Mock TanStack Router hooks
const mockNavigate = vi.fn();
vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
  useLocation: () => ({ pathname: '/ehr-demo' }),
}));

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

    // Default scene is 'encounter', so it should have aria-current="page"
    const encounterBtn = screen.getByRole('button', { name: /encounter/i });
    expect(encounterBtn).toHaveAttribute('aria-current', 'page');

    const fleetBtn = screen.getByRole('button', { name: /fleet/i });
    expect(fleetBtn).not.toHaveAttribute('aria-current');
  });

  it('SceneNav_ClickPill_CallsSetScene', () => {
    renderSceneNav();

    const fleetBtn = screen.getByRole('button', { name: /fleet/i });
    fireEvent.click(fleetBtn);

    // After click, fleet should now be active
    expect(fleetBtn).toHaveAttribute('aria-current', 'page');

    // And encounter should be inactive
    const encounterBtn = screen.getByRole('button', { name: /encounter/i });
    expect(encounterBtn).not.toHaveAttribute('aria-current');
  });

  it('SceneNav_ClickPill_NavigatesToRoute', () => {
    renderSceneNav();

    fireEvent.click(screen.getByRole('button', { name: /fleet/i }));
    expect(mockNavigate).toHaveBeenCalledWith({ to: '/fleet' });

    fireEvent.click(screen.getByRole('button', { name: /case detail/i }));
    expect(mockNavigate).toHaveBeenCalledWith({ to: '/case/demo' });
  });

  it('SceneNav_DemoControls_RendersResetButton', () => {
    renderSceneNav();

    expect(screen.getByRole('button', { name: /reset demo/i })).toBeInTheDocument();
  });
});
