import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SignEncounterButton } from '../SignEncounterButton';

describe('SignEncounterButton', () => {
  it('SignEncounterButton_Click_ShowsSignedStateAndCallsOnSign', () => {
    const onSign = vi.fn();
    render(<SignEncounterButton onSign={onSign} />);

    const button = screen.getByRole('button', { name: /sign encounter/i });
    expect(button).toBeInTheDocument();
    expect(button).toHaveTextContent('Sign Encounter');
    expect(button).not.toBeDisabled();

    fireEvent.click(button);

    expect(onSign).toHaveBeenCalledOnce();
    expect(screen.getByRole('button')).toHaveTextContent('Encounter Signed');
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('SignEncounterButton_AlreadySigned_DisabledWithCheckmark', () => {
    const onSign = vi.fn();
    render(<SignEncounterButton onSign={onSign} signed={true} />);

    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(button).toHaveTextContent('Encounter Signed');
  });
});
