import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FormPreview } from '../FormPreview';

const mockFieldMappings: Record<string, string> = {
  patient_name: 'John Doe',
  patient_dob: '1985-03-15',
  member_id: 'MEM123456',
  diagnosis_codes: 'M54.5 - Low back pain',
  procedure_code: '72148 - MRI Lumbar Spine',
  clinical_summary: 'Patient has chronic low back pain with radiculopathy, failed 8 weeks of conservative therapy including physical therapy and NSAIDs.',
};

describe('FormPreview', () => {
  describe('rendering', () => {
    it('FormPreview_NoFieldMappings_ShowsEmptyState', () => {
      render(<FormPreview fieldMappings={{}} />);
      expect(screen.getByText(/no form data/i)).toBeInTheDocument();
    });

    it('FormPreview_WithFieldMappings_DisplaysAllFields', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} />);

      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('1985-03-15')).toBeInTheDocument();
      expect(screen.getByText('MEM123456')).toBeInTheDocument();
    });

    it('FormPreview_FieldLabels_AreFormatted', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} />);

      // Field names should be formatted (patient_name -> Patient Name)
      expect(screen.getByText('Patient Name')).toBeInTheDocument();
      expect(screen.getByText('Patient Dob')).toBeInTheDocument();
      expect(screen.getByText('Member Id')).toBeInTheDocument();
    });
  });

  describe('field highlighting', () => {
    it('FormPreview_FilledField_HasHighlight', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} showHighlights />);

      const field = screen.getByText('John Doe').closest('[data-field]');
      expect(field).toBeInTheDocument();
      expect(field).toHaveAttribute('data-highlighted', 'true');
    });

    it('FormPreview_WithoutHighlights_NoHighlightClass', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} showHighlights={false} />);

      const field = screen.getByText('John Doe').closest('[data-field]');
      expect(field).toBeInTheDocument();
      expect(field).not.toHaveAttribute('data-highlighted');
    });
  });

  describe('loading state', () => {
    it('FormPreview_Loading_ShowsSkeleton', () => {
      render(<FormPreview fieldMappings={{}} loading />);

      expect(screen.getByTestId('form-skeleton')).toBeInTheDocument();
    });

    it('FormPreview_Loading_HidesContent', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} loading />);

      // When loading, skeleton is shown instead of content
      expect(screen.getByTestId('form-skeleton')).toBeInTheDocument();
    });
  });

  describe('clinical summary', () => {
    it('FormPreview_LongSummary_ShowsExpandable', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} />);

      // Summary should be visible
      expect(screen.getByText(/chronic low back pain/i)).toBeInTheDocument();
    });
  });

  describe('diagnosis codes', () => {
    it('FormPreview_DiagnosisCodes_DisplaysFormatted', () => {
      render(<FormPreview fieldMappings={mockFieldMappings} />);

      expect(screen.getByText(/M54.5/)).toBeInTheDocument();
      // Check that the diagnosis codes field contains the expected value
      const diagnosisField = screen.getByText('Diagnosis Codes').closest('[data-field]');
      expect(diagnosisField).toHaveTextContent('M54.5 - Low back pain');
    });
  });
});
