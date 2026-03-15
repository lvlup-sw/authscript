import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import type { ReactNode } from 'react';
import { DemoProvider, useDemoContext } from '../DemoProvider';

function wrapper({ children }: { children: ReactNode }) {
  return <DemoProvider>{children}</DemoProvider>;
}

describe('DemoProvider', () => {
  it('DemoProvider_DefaultScene_IsEncounter', () => {
    const { result } = renderHook(() => useDemoContext(), { wrapper });
    expect(result.current.scene).toBe('encounter');
  });

  it('DemoProvider_SetScene_UpdatesContext', () => {
    const { result } = renderHook(() => useDemoContext(), { wrapper });
    act(() => {
      result.current.setScene('fleet');
    });
    expect(result.current.scene).toBe('fleet');
  });

  it('DemoProvider_SelectedCaseId_IsNullByDefault', () => {
    const { result } = renderHook(() => useDemoContext(), { wrapper });
    expect(result.current.selectedCaseId).toBeNull();
  });

  it('DemoProvider_SetSelectedCaseId_UpdatesContext', () => {
    const { result } = renderHook(() => useDemoContext(), { wrapper });
    act(() => {
      result.current.setSelectedCaseId('case-123');
    });
    expect(result.current.selectedCaseId).toBe('case-123');
  });
});
