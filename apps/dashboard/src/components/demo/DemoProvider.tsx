import { createContext, useContext, useState } from 'react';
import type { ReactNode } from 'react';

export type DemoScene = 'encounter' | 'fleet' | 'case';

interface DemoContextValue {
  scene: DemoScene;
  setScene: (scene: DemoScene) => void;
  selectedCaseId: string | null;
  setSelectedCaseId: (id: string | null) => void;
}

const DemoContext = createContext<DemoContextValue | null>(null);

export function DemoProvider({ children }: { children: ReactNode }) {
  const [scene, setScene] = useState<DemoScene>('encounter');
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);

  return (
    <DemoContext.Provider value={{ scene, setScene, selectedCaseId, setSelectedCaseId }}>
      {children}
    </DemoContext.Provider>
  );
}

export function useDemoContext(): DemoContextValue {
  const ctx = useContext(DemoContext);
  if (!ctx) {
    throw new Error('useDemoContext must be used within a DemoProvider');
  }
  return ctx;
}
