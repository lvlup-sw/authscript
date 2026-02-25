import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EmbeddedAppFrame } from '../EmbeddedAppFrame';

describe('EmbeddedAppFrame', () => {
  it('EmbeddedAppFrame_Render_ShowsIframeWithCorrectSrc', () => {
    render(<EmbeddedAppFrame src="http://localhost:5173" />);

    const iframe = screen.getByTitle('AuthScript Dashboard');
    expect(iframe).toBeInTheDocument();
    expect(iframe.tagName).toBe('IFRAME');
    expect(iframe).toHaveAttribute('src', 'http://localhost:5173');
    expect(iframe).toHaveAttribute('title');
  });

  it('EmbeddedAppFrame_Hidden_NotVisible', () => {
    const { container } = render(
      <EmbeddedAppFrame src="/" visible={false} />,
    );

    const wrapper = container.querySelector('[data-testid="embedded-frame-wrapper"]');
    expect(wrapper).toBeInTheDocument();
    expect(wrapper).toHaveClass('h-0');
    expect(wrapper).toHaveClass('overflow-hidden');
  });
});
