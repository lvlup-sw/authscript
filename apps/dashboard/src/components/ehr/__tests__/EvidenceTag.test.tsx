import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EvidenceTag } from '../EvidenceTag';

describe('EvidenceTag', () => {
  it('EvidenceTag_Renders_SourceText', () => {
    render(<EvidenceTag source="HPI" />);
    expect(screen.getByText('HPI')).toBeInTheDocument();
  });

  it('EvidenceTag_HasMonospaceStyle', () => {
    const { container } = render(<EvidenceTag source="Assessment" />);
    const tag = container.firstElementChild;
    expect(tag?.className).toContain('font-mono');
  });

  it('EvidenceTag_IsAccessible', () => {
    render(<EvidenceTag source="Problem List" />);
    // Screen reader text
    expect(screen.getByText('Sourced from:')).toBeInTheDocument();
  });
});
