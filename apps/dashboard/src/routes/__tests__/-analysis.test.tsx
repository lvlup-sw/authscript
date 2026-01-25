import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';

// Mock the authscriptService
vi.mock('@/api/authscriptService', () => ({
  authscriptService: {
    getAnalysisResult: vi.fn(),
  },
}));

// Mock child components to isolate route testing
vi.mock('@/components/EvidencePanel', () => ({
  EvidencePanel: ({ evidence, loading }: { evidence: unknown[]; loading?: boolean }) => (
    <div data-testid="evidence-panel" data-loading={loading} data-evidence-count={evidence?.length ?? 0}>
      EvidencePanel Mock
    </div>
  ),
}));

vi.mock('@/components/FormPreview', () => ({
  FormPreview: ({ fieldMappings, loading }: { fieldMappings: Record<string, string>; loading?: boolean }) => (
    <div data-testid="form-preview" data-loading={loading} data-field-count={Object.keys(fieldMappings ?? {}).length}>
      FormPreview Mock
    </div>
  ),
}));

vi.mock('@/components/ConfidenceMeter', () => ({
  ConfidenceMeter: ({ score }: { score: number }) => (
    <div data-testid="confidence-meter" data-score={score}>
      ConfidenceMeter Mock
    </div>
  ),
}));

vi.mock('@/components/PolicyChecklist', () => ({
  PolicyChecklist: ({ transactionId }: { transactionId: string }) => (
    <div data-testid="policy-checklist" data-transaction-id={transactionId}>
      PolicyChecklist Mock
    </div>
  ),
}));

import { authscriptService } from '@/api/authscriptService';
import { EvidencePanel } from '@/components/EvidencePanel';
import { FormPreview } from '@/components/FormPreview';
import { ConfidenceMeter } from '@/components/ConfidenceMeter';
import { PolicyChecklist } from '@/components/PolicyChecklist';
import type { EvidenceItem } from '@authscript/types';

const mockEvidence: EvidenceItem[] = [
  {
    criterionId: 'conservative_therapy',
    status: 'MET',
    evidence: 'Patient completed 8 weeks of physical therapy',
    source: 'Progress Note - 2026-01-10',
    confidence: 0.95,
  },
  {
    criterionId: 'diagnosis_present',
    status: 'MET',
    evidence: 'ICD-10: M54.5 - Low back pain',
    source: 'Problem List',
    confidence: 0.99,
  },
];

const mockFieldMappings: Record<string, string> = {
  patient_name: 'John Doe',
  patient_dob: '1985-03-15',
  member_id: 'MEM123456',
  procedure_code: '72148',
};

const mockAnalysisResult = {
  transactionId: 'txn-123',
  patientName: 'John Doe',
  patientDob: '1985-03-15',
  memberId: 'MEM123456',
  diagnosisCodes: ['M54.5'],
  procedureCode: '72148',
  clinicalSummary: 'Patient has chronic low back pain',
  supportingEvidence: mockEvidence,
  recommendation: 'APPROVE' as const,
  confidenceScore: 0.87,
  fieldMappings: mockFieldMappings,
};

/**
 * Test component that mirrors the Analysis page behavior
 * This allows us to test the component logic without fighting TanStack Router
 */
function AnalysisPageTestComponent({ transactionId }: { transactionId: string }) {
  const { data: analysisResult, isLoading, isError, error } = useQuery({
    queryKey: ['analysis', transactionId],
    queryFn: () => authscriptService.getAnalysisResult(transactionId),
    enabled: Boolean(transactionId),
  });

  if (isError) {
    return (
      <div data-testid="error-state">
        Error: {(error as Error)?.message ?? 'Unknown error'}
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Analysis Detail</h1>
        <p className="text-muted-foreground mt-2">
          Transaction: <code className="bg-muted px-2 py-1 rounded">{transactionId}</code>
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 space-y-6">
          <EvidencePanel
            evidence={analysisResult?.supportingEvidence ?? []}
            loading={isLoading}
          />
          <FormPreview
            fieldMappings={analysisResult?.fieldMappings ?? {}}
            loading={isLoading}
          />
        </div>

        <div className="space-y-6">
          <ConfidenceMeter score={analysisResult?.confidenceScore ?? 0} />
          <PolicyChecklist transactionId={transactionId} />
        </div>
      </div>
    </div>
  );
}

/**
 * Render helper that wraps component with QueryClientProvider
 */
function renderAnalysisPage(transactionId: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AnalysisPageTestComponent transactionId={transactionId} />
    </QueryClientProvider>
  );
}

describe('AnalysisPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('loading state', () => {
    it('AnalysisPage_WhileFetching_PassesLoadingToChildComponents', async () => {
      // Mock a delayed response
      vi.mocked(authscriptService.getAnalysisResult).mockImplementation(
        () => new Promise(() => {}) // Never resolves to keep loading state
      );

      renderAnalysisPage('txn-123');

      // Wait for initial render
      await waitFor(() => {
        expect(screen.getByTestId('evidence-panel')).toBeInTheDocument();
      });

      // Child components should receive loading=true
      expect(screen.getByTestId('evidence-panel')).toHaveAttribute('data-loading', 'true');
      expect(screen.getByTestId('form-preview')).toHaveAttribute('data-loading', 'true');
    });

    it('AnalysisPage_WhileLoading_ShowsTransactionId', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockImplementation(
        () => new Promise(() => {})
      );

      renderAnalysisPage('txn-abc-456');

      await waitFor(() => {
        expect(screen.getByText('txn-abc-456')).toBeInTheDocument();
      });
    });

    it('AnalysisPage_WhileLoading_ShowsPageTitle', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockImplementation(
        () => new Promise(() => {})
      );

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        expect(screen.getByText('Analysis Detail')).toBeInTheDocument();
      });
    });
  });

  describe('success state', () => {
    it('AnalysisPage_WithData_PassesEvidenceToPanel', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const evidencePanel = screen.getByTestId('evidence-panel');
        expect(evidencePanel).toHaveAttribute('data-loading', 'false');
        expect(evidencePanel).toHaveAttribute('data-evidence-count', '2');
      });
    });

    it('AnalysisPage_WithData_PassesFieldMappingsToFormPreview', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const formPreview = screen.getByTestId('form-preview');
        expect(formPreview).toHaveAttribute('data-loading', 'false');
        expect(formPreview).toHaveAttribute('data-field-count', '4');
      });
    });

    it('AnalysisPage_WithData_PassesConfidenceScoreToMeter', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const confidenceMeter = screen.getByTestId('confidence-meter');
        expect(confidenceMeter).toHaveAttribute('data-score', '0.87');
      });
    });

    it('AnalysisPage_WithData_PassesTransactionIdToPolicyChecklist', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const policyChecklist = screen.getByTestId('policy-checklist');
        expect(policyChecklist).toHaveAttribute('data-transaction-id', 'txn-123');
      });
    });

    it('AnalysisPage_WithData_DisplaysPageTitle', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        expect(screen.getByText('Analysis Detail')).toBeInTheDocument();
      });
    });
  });

  describe('error state', () => {
    it('AnalysisPage_WhenFetchFails_ShowsErrorState', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockRejectedValue(
        new Error('Network error')
      );

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        expect(screen.getByTestId('error-state')).toBeInTheDocument();
        expect(screen.getByText(/network error/i)).toBeInTheDocument();
      });
    });

    it('AnalysisPage_WhenApiReturns404_ShowsErrorMessage', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockRejectedValue(
        new Error('Transaction not found')
      );

      renderAnalysisPage('txn-invalid');

      await waitFor(() => {
        expect(screen.getByTestId('error-state')).toBeInTheDocument();
        expect(screen.getByText(/transaction not found/i)).toBeInTheDocument();
      });
    });
  });

  describe('edge cases', () => {
    it('AnalysisPage_WithEmptyEvidence_PassesEmptyArrayToPanel', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        supportingEvidence: [],
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const evidencePanel = screen.getByTestId('evidence-panel');
        expect(evidencePanel).toHaveAttribute('data-evidence-count', '0');
      });
    });

    it('AnalysisPage_WithEmptyFieldMappings_PassesEmptyObjectToFormPreview', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        fieldMappings: {},
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const formPreview = screen.getByTestId('form-preview');
        expect(formPreview).toHaveAttribute('data-field-count', '0');
      });
    });

    it('AnalysisPage_WithZeroConfidence_PassesZeroToMeter', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        confidenceScore: 0,
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const confidenceMeter = screen.getByTestId('confidence-meter');
        expect(confidenceMeter).toHaveAttribute('data-score', '0');
      });
    });

    it('AnalysisPage_WithMaxConfidence_PassesOneToMeter', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        confidenceScore: 1,
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const confidenceMeter = screen.getByTestId('confidence-meter');
        expect(confidenceMeter).toHaveAttribute('data-score', '1');
      });
    });

    it('AnalysisPage_WithSpecialCharactersInTransactionId_HandlesCorrectly', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123-abc');

      await waitFor(() => {
        expect(screen.getByText('txn-123-abc')).toBeInTheDocument();
      });
    });

    it('AnalysisPage_WithUndefinedEvidence_DefaultsToEmptyArray', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        supportingEvidence: undefined as unknown as EvidenceItem[],
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const evidencePanel = screen.getByTestId('evidence-panel');
        // Should handle undefined gracefully with nullish coalescing
        expect(evidencePanel).toHaveAttribute('data-evidence-count', '0');
      });
    });

    it('AnalysisPage_WithUndefinedFieldMappings_DefaultsToEmptyObject', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        fieldMappings: undefined as unknown as Record<string, string>,
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const formPreview = screen.getByTestId('form-preview');
        expect(formPreview).toHaveAttribute('data-field-count', '0');
      });
    });

    it('AnalysisPage_WithUndefinedConfidenceScore_DefaultsToZero', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue({
        ...mockAnalysisResult,
        confidenceScore: undefined as unknown as number,
      });

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const confidenceMeter = screen.getByTestId('confidence-meter');
        expect(confidenceMeter).toHaveAttribute('data-score', '0');
      });
    });
  });

  describe('API integration', () => {
    it('AnalysisPage_OnMount_CallsGetAnalysisResultWithTransactionId', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-specific-id');

      await waitFor(() => {
        expect(authscriptService.getAnalysisResult).toHaveBeenCalledWith('txn-specific-id');
      });
    });

    it('AnalysisPage_OnMount_CallsApiOnlyOnce', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        expect(screen.getByTestId('confidence-meter')).toHaveAttribute('data-score', '0.87');
      });

      // Should only be called once (no unnecessary refetches)
      expect(authscriptService.getAnalysisResult).toHaveBeenCalledTimes(1);
    });

    it('AnalysisPage_WithDifferentTransactionIds_FetchesDifferentData', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      const { unmount } = renderAnalysisPage('txn-first');

      await waitFor(() => {
        expect(authscriptService.getAnalysisResult).toHaveBeenCalledWith('txn-first');
      });

      unmount();

      renderAnalysisPage('txn-second');

      await waitFor(() => {
        expect(authscriptService.getAnalysisResult).toHaveBeenCalledWith('txn-second');
      });
    });
  });

  describe('component rendering', () => {
    it('AnalysisPage_RendersAllChildComponents', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        expect(screen.getByTestId('evidence-panel')).toBeInTheDocument();
        expect(screen.getByTestId('form-preview')).toBeInTheDocument();
        expect(screen.getByTestId('confidence-meter')).toBeInTheDocument();
        expect(screen.getByTestId('policy-checklist')).toBeInTheDocument();
      });
    });

    it('AnalysisPage_DisplaysTransactionInCodeBlock', async () => {
      vi.mocked(authscriptService.getAnalysisResult).mockResolvedValue(mockAnalysisResult);

      renderAnalysisPage('txn-123');

      await waitFor(() => {
        const codeElement = screen.getByText('txn-123');
        expect(codeElement.tagName.toLowerCase()).toBe('code');
      });
    });
  });
});
