import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { PARequestCard, type PARequest } from '../PARequestCard';

// Mock @tanstack/react-router Link component
vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: Record<string, string> }) => (
    <a href={`${to}/${params?.transactionId ?? ''}`} data-testid="router-link">
      {children}
    </a>
  ),
}));

function createMockPARequest(overrides: Partial<PARequest> = {}): PARequest {
  return {
    id: 'PA-001',
    patientName: 'Jane Smith',
    patientId: 'MRN-12345',
    procedureCode: '72148',
    procedureName: 'MRI Lumbar Spine',
    payer: 'Blue Cross Blue Shield',
    currentStep: 'process',
    createdAt: new Date().toISOString(),
    encounterId: 'ENC-001',
    ...overrides,
  };
}

describe('PARequestCard', () => {
  describe('rendering', () => {
    it('PARequestCard_WithValidRequest_DisplaysPatientInfo', () => {
      const request = createMockPARequest();
      render(<PARequestCard request={request} />);

      expect(screen.getByText('Jane Smith')).toBeInTheDocument();
      expect(screen.getByText(/MRN-12345/)).toBeInTheDocument();
    });

    it('PARequestCard_WithValidRequest_DisplaysProcedure', () => {
      const request = createMockPARequest();
      render(<PARequestCard request={request} />);

      expect(screen.getByText('72148')).toBeInTheDocument();
      expect(screen.getByText('MRI Lumbar Spine')).toBeInTheDocument();
    });

    it('PARequestCard_WithValidRequest_DisplaysPayer', () => {
      const request = createMockPARequest();
      render(<PARequestCard request={request} />);

      expect(screen.getByText('Blue Cross Blue Shield')).toBeInTheDocument();
    });
  });

  describe('confidence display', () => {
    it('PARequestCard_WithHighConfidence_DisplaysPercentage', () => {
      const request = createMockPARequest({ confidenceScore: 0.92 });
      render(<PARequestCard request={request} />);

      expect(screen.getByText(/92%/)).toBeInTheDocument();
    });

    it('PARequestCard_WithMediumConfidence_DisplaysPercentage', () => {
      const request = createMockPARequest({ confidenceScore: 0.65 });
      render(<PARequestCard request={request} />);

      expect(screen.getByText(/65%/)).toBeInTheDocument();
    });

    it('PARequestCard_WithLowConfidence_DisplaysReviewBadge', () => {
      const request = createMockPARequest({ confidenceScore: 0.42 });
      render(<PARequestCard request={request} />);

      // Low confidence (< 0.5) shows "Review" text in a badge (data-slot="badge")
      const badges = screen.getAllByText('Review');
      const confidenceBadge = badges.find(el => el.getAttribute('data-slot') === 'badge');
      expect(confidenceBadge).toBeDefined();
    });

    it('PARequestCard_WithUndefinedConfidence_ShowsNoConfidenceBadge', () => {
      const request = createMockPARequest({ confidenceScore: undefined });
      render(<PARequestCard request={request} />);

      // No confidence percentage badge should render
      expect(screen.queryByText(/\d+%/)).not.toBeInTheDocument();
      // The only "Review" text should be from the WorkflowProgress step, not a badge
      const reviewElements = screen.queryAllByText('Review');
      const confidenceBadge = reviewElements.find(el => el.getAttribute('data-slot') === 'badge');
      expect(confidenceBadge).toBeUndefined();
    });

    it('PARequestCard_WithRealConfidenceRange_DisplaysCorrectly', () => {
      // Test with various realistic confidence values from Intelligence
      const request = createMockPARequest({ confidenceScore: 0.78 });
      render(<PARequestCard request={request} />);

      expect(screen.getByText(/78%/)).toBeInTheDocument();
    });
  });

  describe('workflow states', () => {
    it('PARequestCard_InProcessingState_ShowsProcessingIndicator', () => {
      const request = createMockPARequest({ currentStep: 'process' });
      render(<PARequestCard request={request} />);

      expect(screen.getByText('Processing...')).toBeInTheDocument();
    });

    it('PARequestCard_InDeliverState_ShowsReviewButton', () => {
      const request = createMockPARequest({ currentStep: 'deliver' });
      render(<PARequestCard request={request} />);

      expect(screen.getByText('Review & Confirm')).toBeInTheDocument();
    });

    it('PARequestCard_InReviewCompletedState_ShowsSubmittedMessage', () => {
      const request = createMockPARequest({
        currentStep: 'review',
        stepStatuses: { review: 'completed' },
      });
      render(<PARequestCard request={request} />);

      expect(screen.getByText('Submitted to athenahealth')).toBeInTheDocument();
    });
  });

  describe('attention flag', () => {
    it('PARequestCard_RequiresAttention_HasWarningRing', () => {
      const request = createMockPARequest({ requiresAttention: true });
      const { container } = render(<PARequestCard request={request} />);

      const card = container.firstChild as HTMLElement;
      expect(card.className).toMatch(/ring-warning/);
    });
  });

  describe('callbacks', () => {
    it('PARequestCard_OnReviewClick_CallsCallback', () => {
      const onReview = vi.fn();
      const request = createMockPARequest({ currentStep: 'deliver' });
      render(<PARequestCard request={request} onReview={onReview} />);

      fireEvent.click(screen.getByText('Review & Confirm'));

      expect(onReview).toHaveBeenCalledWith('PA-001');
    });
  });
});
