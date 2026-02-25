import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EhrDemoPage } from '../ehr-demo';

describe('EhrDemoPage', () => {
  it('EhrDemoPage_Render_ShowsAllSections', () => {
    render(<EhrDemoPage />);

    // EhrHeader renders athenaOne branding
    expect(screen.getByText('athenaOne')).toBeInTheDocument();

    // Patient name visible
    expect(screen.getByText('Maria Garcia')).toBeInTheDocument();

    // EncounterNote shows Chief Complaint section
    expect(screen.getByText('Chief Complaint')).toBeInTheDocument();

    // Plan includes MRI lumbar spine
    expect(screen.getByText(/MRI lumbar spine/)).toBeInTheDocument();

    // Sign Encounter button visible
    expect(screen.getByRole('button', { name: 'Sign Encounter' })).toBeInTheDocument();

    // EmbeddedAppFrame should NOT be visible initially (no iframe in DOM)
    expect(screen.queryByTitle('AuthScript Dashboard')).not.toBeInTheDocument();
  });

  it('EhrDemoPage_SignEncounter_ShowsEmbeddedDashboard', () => {
    render(<EhrDemoPage />);

    // Click "Sign Encounter"
    const signButton = screen.getByRole('button', { name: 'Sign Encounter' });
    fireEvent.click(signButton);

    // Button changes to "Encounter Signed" and is disabled
    const signedButton = screen.getByRole('button', { name: 'Encounter Signed' });
    expect(signedButton).toBeDisabled();

    // EmbeddedAppFrame becomes visible — look for the iframe
    const iframe = screen.getByTitle('AuthScript Dashboard');
    expect(iframe).toBeInTheDocument();
    expect(iframe.tagName.toLowerCase()).toBe('iframe');
    expect(iframe).toHaveAttribute('src', expect.stringContaining('quickDemo=true'));
  });
});
