import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { PolicyCriteriaModal } from '../PolicyCriteriaModal';

// Mock createPortal so the dialog renders inline
vi.mock('react-dom', async () => {
  const actual = await vi.importActual<typeof import('react-dom')>('react-dom');
  return {
    ...actual,
    createPortal: (children: React.ReactNode) => children,
  };
});

describe('PolicyCriteriaModal', () => {
  it('PolicyCriteriaModal_Closed_RendersNothing', () => {
    const { container } = render(
      <PolicyCriteriaModal isOpen={false} onClose={vi.fn()} />,
    );
    expect(container.innerHTML).toBe('');
  });

  it('PolicyCriteriaModal_Open_ShowsPolicyHeader', () => {
    render(<PolicyCriteriaModal isOpen={true} onClose={vi.fn()} />);

    expect(screen.getByText('Policy Criteria')).toBeInTheDocument();
    expect(screen.getByText(/LCD L34220/)).toBeInTheDocument();
    expect(screen.getByText(/MRI without Contrast, Lumbar Spine/)).toBeInTheDocument();
  });

  it('PolicyCriteriaModal_Open_ShowsAllFiveCriteria', () => {
    render(<PolicyCriteriaModal isOpen={true} onClose={vi.fn()} />);

    expect(screen.getByText('Valid ICD-10 for lumbar pathology')).toBeInTheDocument();
    expect(screen.getByText(/Red flag symptoms/)).toBeInTheDocument();
    expect(screen.getByText('4+ weeks conservative management')).toBeInTheDocument();
    expect(screen.getByText('Clinical rationale documented')).toBeInTheDocument();
    expect(screen.getByText('No recent duplicative imaging')).toBeInTheDocument();

    // Numbered 1-5
    const listItems = screen.getAllByRole('listitem');
    expect(listItems).toHaveLength(5);
  });

  it('PolicyCriteriaModal_Open_ShowsInfoBanner', () => {
    render(<PolicyCriteriaModal isOpen={true} onClose={vi.fn()} />);

    expect(screen.getByText(/Blue Cross Blue Shield/)).toBeInTheDocument();
    expect(screen.getByText(/CPT 72148/)).toBeInTheDocument();
    expect(screen.getByText(/AuthScript will automatically verify/)).toBeInTheDocument();
  });

  it('PolicyCriteriaModal_Close_CallsOnClose', () => {
    const onClose = vi.fn();
    render(<PolicyCriteriaModal isOpen={true} onClose={onClose} />);

    // Both X button and footer Close button call onClose
    const closeButtons = screen.getAllByRole('button', { name: 'Close' });
    expect(closeButtons).toHaveLength(2);

    fireEvent.click(closeButtons[1]); // Footer Close button
    expect(onClose).toHaveBeenCalledOnce();

    fireEvent.click(closeButtons[0]); // X button
    expect(onClose).toHaveBeenCalledTimes(2);
  });
});
