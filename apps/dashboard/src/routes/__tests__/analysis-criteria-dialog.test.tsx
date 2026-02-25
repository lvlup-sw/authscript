import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { CriteriaReasonDialog } from '../analysis.$transactionId';

// Mock createPortal so the dialog renders inline instead of into document.body
vi.mock('react-dom', async () => {
  const actual = await vi.importActual<typeof import('react-dom')>('react-dom');
  return {
    ...actual,
    createPortal: (children: React.ReactNode) => children,
  };
});

// Mock TanStack Router to avoid route registration errors
vi.mock('@tanstack/react-router', () => ({
  createFileRoute: () => () => ({}),
  Link: ({ children, ...props }: { children: React.ReactNode; to: string }) => (
    <a {...props}>{children}</a>
  ),
  useNavigate: () => vi.fn(),
}));

// Mock the API service hooks
vi.mock('@/api/graphqlService', () => ({
  usePARequest: () => ({ data: null, isLoading: false }),
  useUpdatePARequest: () => ({ mutateAsync: vi.fn() }),
  useSubmitPARequest: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useAddReviewTime: () => ({ mutate: vi.fn() }),
}));

describe('CriteriaReasonDialog', () => {
  const multiSentenceReason =
    'M54.5 (Low back pain) is a covered diagnosis code. Progressive neurological deficit: L5 weakness, diminished reflexes. Bypasses conservative therapy requirement per LCD.';

  function renderDialog(overrides: Partial<Parameters<typeof CriteriaReasonDialog>[0]> = {}) {
    const defaultProps = {
      isOpen: true,
      onClose: vi.fn(),
      met: true as boolean | null,
      label: 'Diagnosis is covered',
      reason: multiSentenceReason,
    };
    return render(<CriteriaReasonDialog {...defaultProps} {...overrides} />);
  }

  it('CriteriaReasonDialog_Render_ShowsStructuredReasoning', () => {
    // Arrange & Act
    renderDialog();

    // Assert — the reason should NOT be a single <p> blob
    // It should be broken into multiple items (list items or separate elements)
    const aiSection = screen.getByText('AI Analysis').closest('div')!;
    const container = aiSection.parentElement!;

    // There should be NO single <p> containing the full reason text
    const allParagraphs = container.querySelectorAll('p');
    const fullReasonParagraph = Array.from(allParagraphs).find(
      (p) => p.textContent === multiSentenceReason,
    );
    expect(fullReasonParagraph).toBeUndefined();

    // There should be multiple list items or separate elements for evidence
    const listItems = container.querySelectorAll('li');
    expect(listItems.length).toBeGreaterThanOrEqual(1);

    // Status badge should be visible
    expect(screen.getByText('Criterion Met')).toBeInTheDocument();

    // Criterion label should be visible
    expect(screen.getByText('Diagnosis is covered')).toBeInTheDocument();

    // AI Analysis heading should be visible
    expect(screen.getByText('AI Analysis')).toBeInTheDocument();
  });

  it('CriteriaReasonDialog_Met_HighlightsKeyEvidence', () => {
    // Arrange & Act
    renderDialog({ met: true });

    // Assert — the first sentence should be rendered as a bold/distinct summary
    const summaryText = 'M54.5 (Low back pain) is a covered diagnosis code.';
    const summaryElement = screen.getByText(summaryText);
    expect(summaryElement).toBeInTheDocument();

    // The summary should be in a distinct element (e.g. bold or a different tag)
    // than the evidence list items
    const isBold =
      summaryElement.tagName === 'STRONG' ||
      summaryElement.classList.contains('font-semibold') ||
      summaryElement.classList.contains('font-bold');
    expect(isBold).toBe(true);

    // Evidence items should be separate from the summary
    const listItems = screen.getAllByRole('listitem');
    expect(listItems.length).toBeGreaterThanOrEqual(1);

    // Evidence should contain the remaining sentences
    const evidenceTexts = listItems.map((li) => li.textContent);
    expect(evidenceTexts.some((t) => t?.includes('Progressive neurological deficit'))).toBe(true);
    expect(evidenceTexts.some((t) => t?.includes('Bypasses conservative therapy'))).toBe(true);
  });

  it('CriteriaReasonDialog_NotMet_ShowsGapIndicator', () => {
    // Arrange
    const notMetReason =
      'No prior head CT has been performed. I48.91 is a cardiovascular code, not a primary neurological diagnosis.';

    // Act
    renderDialog({ met: false, reason: notMetReason });

    // Assert — there should be a visible gap/action indicator
    const gapIndicator = screen.getByText((content) => {
      const lower = content.toLowerCase();
      return (
        lower.includes('action needed') ||
        lower.includes('documentation gap') ||
        lower.includes('missing')
      );
    });
    expect(gapIndicator).toBeInTheDocument();

    // Status badge should show "Criterion Not Met"
    expect(screen.getByText('Criterion Not Met')).toBeInTheDocument();
  });
});
