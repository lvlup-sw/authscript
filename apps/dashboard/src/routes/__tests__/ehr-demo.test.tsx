import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { createElement } from 'react';

// Mock PdfViewerModal to avoid pdf-lib dependency in tests
vi.mock('@/components/PdfViewerModal', () => ({
  PdfViewerModal: ({
    isOpen,
    title,
    staticUrl,
  }: {
    isOpen: boolean;
    title?: string;
    staticUrl?: string;
  }) =>
    isOpen
      ? createElement(
          'div',
          {
            'data-testid': staticUrl ? 'pdf-viewer-static' : 'pdf-viewer',
          },
          title ?? 'PDF Viewer',
        )
      : null,
}));

// Mock PolicyCriteriaModal to avoid portal issues
vi.mock('@/components/ehr/PolicyCriteriaModal', () => ({
  PolicyCriteriaModal: ({ isOpen }: { isOpen: boolean }) =>
    isOpen
      ? createElement('div', { 'data-testid': 'policy-criteria-modal' }, 'Policy Criteria')
      : null,
}));

async function renderEhrDemoPage() {
  const { EhrDemoPage } = await import('../ehr-demo');
  return render(createElement(EhrDemoPage));
}

describe('EhrDemoPage', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('EhrDemoPage_Render_ShowsAllSections', async () => {
    await renderEhrDemoPage();

    // EhrHeader renders athenaOne branding
    expect(screen.getByText('athenaOne')).toBeInTheDocument();

    // Patient name visible
    expect(screen.getByText('Rebecca Sandbox')).toBeInTheDocument();

    // Encounter metadata visible
    expect(screen.getByText('Dr. Kelli Smith')).toBeInTheDocument();
    expect(screen.getByText('Family Medicine')).toBeInTheDocument();

    // EncounterNote shows sections
    expect(screen.getByText('Encounter Note')).toBeInTheDocument();

    // Vitals visible
    expect(screen.getByText('128/82')).toBeInTheDocument();
    expect(screen.getByText('72')).toBeInTheDocument();

    // Orders visible
    expect(screen.getByText('72148')).toBeInTheDocument();
    expect(screen.getByText('Requires PA')).toBeInTheDocument();

    // Sign Encounter button visible
    expect(screen.getByRole('button', { name: 'Sign Encounter' })).toBeInTheDocument();

    // Pre-sign action buttons visible
    expect(screen.getByRole('button', { name: /preview pa form/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /view policy criteria/i })).toBeInTheDocument();

    // NO iframe in DOM
    expect(screen.queryByTitle('AuthScript Dashboard')).not.toBeInTheDocument();
    expect(document.querySelector('iframe')).toBeNull();

    // No PAResultsPanel visible initially
    expect(screen.queryByText('AuthScript — Prior Authorization')).not.toBeInTheDocument();

    // No PA sidebar stages initially
    expect(screen.queryByText('Prior Auth')).not.toBeInTheDocument();
  });

  it('EhrDemoPage_PreviewPAForm_OpensBlankForm', async () => {
    await renderEhrDemoPage();

    fireEvent.click(screen.getByRole('button', { name: /preview pa form/i }));

    // Should open the static PDF viewer (blank form)
    expect(screen.getByTestId('pdf-viewer-static')).toBeInTheDocument();
    expect(screen.getByText(/NOFR001/)).toBeInTheDocument();
  });

  it('EhrDemoPage_ViewPolicyCriteria_OpensModal', async () => {
    await renderEhrDemoPage();

    fireEvent.click(screen.getByRole('button', { name: /view policy criteria/i }));

    expect(screen.getByTestId('policy-criteria-modal')).toBeInTheDocument();
  });

  it('EhrDemoPage_Sign_ShowsProcessingPanel', async () => {
    await renderEhrDemoPage();

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));
    });

    // PA panel header appears
    expect(screen.getByText('AuthScript — Prior Authorization')).toBeInTheDocument();

    // Sign button becomes disabled
    expect(screen.getByRole('button', { name: 'Encounter Signed' })).toBeDisabled();
  });

  it('EhrDemoPage_Sign_OrderStatusUpdates', async () => {
    await renderEhrDemoPage();

    // Initially "Requires PA"
    expect(screen.getByText('Requires PA')).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));
    });

    // After signing, order status changes to "Pending"
    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.queryByText('Requires PA')).not.toBeInTheDocument();
  });

  it('EhrDemoPage_FullFlow_EndToEnd', async () => {
    await renderEhrDemoPage();

    // Sign encounter
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));
    });

    // Advance signing delay (800ms)
    await act(async () => {
      vi.advanceTimersByTime(800);
    });

    // Advance processing delay (5000ms)
    await act(async () => {
      vi.advanceTimersByTime(5000);
    });

    // Confidence score from DEMO_PA_RESULT
    expect(screen.getByText('88%')).toBeInTheDocument();

    // LCD L34220 criteria visible
    expect(screen.getByText('Valid ICD-10 for lumbar pathology')).toBeInTheDocument();
    expect(screen.getByText('4+ weeks conservative management documented')).toBeInTheDocument();
    expect(screen.getByText('No recent duplicative CT/MRI')).toBeInTheDocument();

    // Submit button visible
    expect(screen.getByRole('button', { name: /submit to blue cross/i })).toBeInTheDocument();

    // View PA Form button visible (in PAResultsPanel)
    expect(screen.getByRole('button', { name: /^view pa form$/i })).toBeInTheDocument();

    // Order status shows completed
    expect(screen.getByText('Completed')).toBeInTheDocument();
  });

  it('EhrDemoPage_Sidebar_RendersAllStages', async () => {
    await renderEhrDemoPage();

    // Encounter stages visible (no PA stages when idle)
    expect(screen.getByText('HPI')).toBeInTheDocument();
    expect(screen.getByText('ROS')).toBeInTheDocument();
    expect(screen.getByText('PE')).toBeInTheDocument();
    expect(screen.getByText('A&P')).toBeInTheDocument();
    // No Sign-Off stage
    expect(screen.queryByText('Sign-Off')).not.toBeInTheDocument();
  });

  it('EhrDemoPage_Signed_SidebarShowsPAStages', async () => {
    await renderEhrDemoPage();

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));
    });

    // PA sidebar stages appear
    expect(screen.getByText('Prior Auth')).toBeInTheDocument();
    expect(screen.getByText('Analyzing')).toBeInTheDocument();
    expect(screen.getByText('Submit')).toBeInTheDocument();
    expect(screen.getByText('Complete')).toBeInTheDocument();

    // All encounter stages completed after signing
    const apContainer = screen.getByText('A&P').closest('[data-stage]');
    expect(apContainer).toHaveAttribute('data-completed', 'true');
  });

  it('EhrDemoPage_ViewPdf_OpensPdfViewer', async () => {
    await renderEhrDemoPage();

    // Sign and advance to reviewing state
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Sign Encounter' }));
    });

    await act(async () => {
      vi.advanceTimersByTime(800);
    });

    await act(async () => {
      vi.advanceTimersByTime(5000);
    });

    // Click "View PA Form" in the results panel (not "Preview PA Form")
    fireEvent.click(screen.getByRole('button', { name: /^view pa form$/i }));

    expect(screen.getByTestId('pdf-viewer')).toBeInTheDocument();
  });
});
