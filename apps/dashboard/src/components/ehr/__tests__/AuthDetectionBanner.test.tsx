import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

// Mock motion/react for test environment
vi.mock('motion/react', () => ({
  motion: {
    div: ({ children, ...props }: any) => {
      const {
        initial, animate: _animate, exit, transition,
        ...domProps
      } = props;
      return <div {...domProps}>{children}</div>;
    },
  },
  AnimatePresence: ({ children }: any) => <>{children}</>,
}));

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
