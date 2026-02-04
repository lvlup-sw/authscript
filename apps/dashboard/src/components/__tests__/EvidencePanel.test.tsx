import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EvidencePanel } from '../EvidencePanel';
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
  {
    criterionId: 'failed_treatment',
    status: 'UNCLEAR',
    evidence: 'Documentation mentions "ongoing pain" but no explicit treatment failure',
    source: 'Clinical Notes',
    confidence: 0.65,
  },
  {
    criterionId: 'neurological_symptoms',
    status: 'NOT_MET',
    evidence: 'No neurological deficits documented',
    source: 'Physical Exam',
    confidence: 0.88,
  },
];

describe('EvidencePanel', () => {
  describe('rendering', () => {
    it('EvidencePanel_NoEvidence_ShowsEmptyState', () => {
      render(<EvidencePanel evidence={[]} />);
      expect(screen.getByText(/no evidence/i)).toBeInTheDocument();
    });

    it('EvidencePanel_WithEvidence_DisplaysAllItems', () => {
      render(<EvidencePanel evidence={mockEvidence} />);

      // Criterion IDs are formatted (conservative_therapy -> Conservative Therapy)
      expect(screen.getByText('Conservative Therapy')).toBeInTheDocument();
      expect(screen.getByText('Diagnosis Present')).toBeInTheDocument();
      expect(screen.getByText('Failed Treatment')).toBeInTheDocument();
      expect(screen.getByText('Neurological Symptoms')).toBeInTheDocument();
    });
  });

  describe('status badges', () => {
    it('EvidencePanel_MetStatus_ShowsGreenBadge', () => {
      render(<EvidencePanel evidence={[mockEvidence[0]]} />);

      const badge = screen.getByText('Met');
      expect(badge).toBeInTheDocument();
      expect(badge.className).toMatch(/success/);
    });

    it('EvidencePanel_NotMetStatus_ShowsRedBadge', () => {
      render(<EvidencePanel evidence={[mockEvidence[3]]} />);

      const badge = screen.getByText('Not Met');
      expect(badge).toBeInTheDocument();
      expect(badge.className).toMatch(/destructive/);
    });

    it('EvidencePanel_UnclearStatus_ShowsYellowBadge', () => {
      render(<EvidencePanel evidence={[mockEvidence[2]]} />);

      const badge = screen.getByText('Unclear');
      expect(badge).toBeInTheDocument();
      expect(badge.className).toMatch(/warning/);
    });
  });

  describe('evidence content', () => {
    it('EvidencePanel_EvidenceItem_DisplaysEvidenceText', () => {
      render(<EvidencePanel evidence={[mockEvidence[0]]} />);

      expect(screen.getByText(/patient completed 8 weeks/i)).toBeInTheDocument();
    });

    it('EvidencePanel_EvidenceItem_DisplaysSource', () => {
      render(<EvidencePanel evidence={[mockEvidence[0]]} />);

      expect(screen.getByText(/progress note/i)).toBeInTheDocument();
    });

    it('EvidencePanel_EvidenceItem_DisplaysConfidence', () => {
      render(<EvidencePanel evidence={[mockEvidence[0]]} />);

      expect(screen.getByText(/95%/)).toBeInTheDocument();
    });
  });

  describe('summary', () => {
    it('EvidencePanel_MultipleItems_ShowsSummaryCount', () => {
      render(<EvidencePanel evidence={mockEvidence} />);

      // Summary shows total count and status breakdown (2 met, 1 not met, 1 unclear)
      expect(screen.getByText('4 criteria analyzed')).toBeInTheDocument();
      expect(screen.getByText('2')).toBeInTheDocument();
      const ones = screen.getAllByText('1');
      expect(ones.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('loading state', () => {
    it('EvidencePanel_Loading_ShowsSkeleton', () => {
      render(<EvidencePanel evidence={[]} loading />);

      expect(screen.getByTestId('evidence-skeleton')).toBeInTheDocument();
    });
  });
});
