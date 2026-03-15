import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AuthDetectionBanner } from '../AuthDetectionBanner';

describe('AuthDetectionBanner', () => {
  const defaultProps = {
    visible: true,
    payer: 'Aetna',
    policyId: 'LCD L34220',
    cptCode: '72148',
  };

  it('AuthDetectionBanner_WhenVisible_ShowsPAMessage', () => {
    render(<AuthDetectionBanner {...defaultProps} />);

    expect(screen.getByText(/PA Required/)).toBeInTheDocument();
  });

  it('AuthDetectionBanner_WhenHidden_RendersNothing', () => {
    const { container } = render(
      <AuthDetectionBanner {...defaultProps} visible={false} />,
    );

    expect(container.innerHTML).toBe('');
  });

  it('AuthDetectionBanner_RendersPayerName', () => {
    render(<AuthDetectionBanner {...defaultProps} />);

    expect(screen.getByText(/Aetna/)).toBeInTheDocument();
  });

  it('AuthDetectionBanner_RendersPolicyReference', () => {
    render(<AuthDetectionBanner {...defaultProps} />);

    expect(screen.getByText(/LCD L34220/)).toBeInTheDocument();
  });
});
