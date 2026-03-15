import { describe, it, expect } from 'vitest';
import { getInitials, getConfidenceColor } from '../formatUtils';

describe('getInitials', () => {
  it('extracts two initials from full name', () => {
    expect(getInitials('Rebecca Sandbox')).toBe('RS');
  });

  it('handles single name', () => {
    expect(getInitials('Madonna')).toBe('M');
  });

  it('handles three-part name', () => {
    expect(getInitials('John Paul Jones')).toBe('JP');
  });

  it('uppercases lowercase input', () => {
    expect(getInitials('john doe')).toBe('JD');
  });
});

describe('getConfidenceColor', () => {
  it('returns green for >= 80', () => {
    expect(getConfidenceColor(80)).toBe('text-green-600');
    expect(getConfidenceColor(93)).toBe('text-green-600');
    expect(getConfidenceColor(100)).toBe('text-green-600');
  });

  it('returns amber for >= 60 and < 80', () => {
    expect(getConfidenceColor(60)).toBe('text-amber-600');
    expect(getConfidenceColor(70)).toBe('text-amber-600');
    expect(getConfidenceColor(79)).toBe('text-amber-600');
  });

  it('returns red for < 60', () => {
    expect(getConfidenceColor(59)).toBe('text-red-600');
    expect(getConfidenceColor(30)).toBe('text-red-600');
    expect(getConfidenceColor(0)).toBe('text-red-600');
  });
});
