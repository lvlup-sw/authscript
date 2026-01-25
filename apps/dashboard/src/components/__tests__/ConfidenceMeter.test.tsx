import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ConfidenceMeter } from '../ConfidenceMeter';

describe('ConfidenceMeter', () => {
  describe('rendering', () => {
    it('ConfidenceMeter_ValidScore_DisplaysPercentage', () => {
      render(<ConfidenceMeter score={0.85} />);
      expect(screen.getByText('85%')).toBeInTheDocument();
    });

    it('ConfidenceMeter_ZeroScore_DisplaysZero', () => {
      render(<ConfidenceMeter score={0} />);
      expect(screen.getByText('0%')).toBeInTheDocument();
    });

    it('ConfidenceMeter_FullScore_DisplaysHundred', () => {
      render(<ConfidenceMeter score={1} />);
      expect(screen.getByText('100%')).toBeInTheDocument();
    });
  });

  describe('color coding', () => {
    it('ConfidenceMeter_HighConfidence_ShowsGreen', () => {
      const { container } = render(<ConfidenceMeter score={0.9} />);
      const meter = container.querySelector('[data-testid="confidence-fill"]');
      expect(meter).toHaveClass('bg-[hsl(var(--success))]');
    });

    it('ConfidenceMeter_MediumConfidence_ShowsYellow', () => {
      const { container } = render(<ConfidenceMeter score={0.6} />);
      const meter = container.querySelector('[data-testid="confidence-fill"]');
      expect(meter).toHaveClass('bg-yellow-500');
    });

    it('ConfidenceMeter_LowConfidence_ShowsRed', () => {
      const { container } = render(<ConfidenceMeter score={0.3} />);
      const meter = container.querySelector('[data-testid="confidence-fill"]');
      expect(meter).toHaveClass('bg-red-500');
    });
  });

  describe('labels', () => {
    it('ConfidenceMeter_HighConfidence_ShowsHighLabel', () => {
      render(<ConfidenceMeter score={0.9} showLabel />);
      expect(screen.getByText(/high confidence/i)).toBeInTheDocument();
    });

    it('ConfidenceMeter_MediumConfidence_ShowsMediumLabel', () => {
      render(<ConfidenceMeter score={0.6} showLabel />);
      expect(screen.getByText(/medium confidence/i)).toBeInTheDocument();
    });

    it('ConfidenceMeter_LowConfidence_ShowsLowLabel', () => {
      render(<ConfidenceMeter score={0.3} showLabel />);
      expect(screen.getByText(/low confidence/i)).toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('ConfidenceMeter_HasAriaLabel', () => {
      render(<ConfidenceMeter score={0.75} />);
      const meter = screen.getByRole('meter');
      expect(meter).toHaveAttribute('aria-valuenow', '75');
      expect(meter).toHaveAttribute('aria-valuemin', '0');
      expect(meter).toHaveAttribute('aria-valuemax', '100');
    });

    it('ConfidenceMeter_HasAccessibleName', () => {
      render(<ConfidenceMeter score={0.75} />);
      const meter = screen.getByRole('meter');
      expect(meter).toHaveAttribute('aria-label', expect.stringContaining('confidence'));
    });
  });

  describe('variants', () => {
    it('ConfidenceMeter_CompactVariant_HasSmallerSize', () => {
      const { container } = render(<ConfidenceMeter score={0.8} variant="compact" />);
      const meter = container.querySelector('[data-testid="confidence-container"]');
      expect(meter).toHaveClass('h-2');
    });

    it('ConfidenceMeter_DefaultVariant_HasNormalSize', () => {
      const { container } = render(<ConfidenceMeter score={0.8} />);
      const meter = container.querySelector('[data-testid="confidence-container"]');
      expect(meter).toHaveClass('h-4');
    });
  });

  describe('edge cases', () => {
    it('ConfidenceMeter_NegativeScore_ClampsToZero', () => {
      render(<ConfidenceMeter score={-0.5} />);
      expect(screen.getByText('0%')).toBeInTheDocument();
    });

    it('ConfidenceMeter_OverOneScore_ClampsToHundred', () => {
      render(<ConfidenceMeter score={1.5} />);
      expect(screen.getByText('100%')).toBeInTheDocument();
    });

    it('ConfidenceMeter_UndefinedScore_ShowsZero', () => {
      render(<ConfidenceMeter score={undefined as unknown as number} />);
      expect(screen.getByText('0%')).toBeInTheDocument();
    });
  });
});
