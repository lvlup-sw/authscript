import {
  createContext,
  useState,
  useEffect,
  useCallback,
  useRef,
  type ReactNode,
} from 'react';

export type Scene = 'encounter' | 'fleet' | 'case';

const SCENE_ORDER: Scene[] = ['encounter', 'fleet', 'case'];
const AUTO_PLAY_INTERVAL_MS = 15_000;

export interface DemoContextValue {
  scene: Scene;
  setScene: (scene: Scene) => void;
  selectedCaseId: string | null;
  setSelectedCaseId: (id: string | null) => void;
  autoPlay: boolean;
  setAutoPlay: (v: boolean) => void;
  resetDemo: () => void;
}

export const DemoContext = createContext<DemoContextValue | null>(null);

interface DemoProviderProps {
  children: ReactNode;
  autoPlay?: boolean;
}

export function DemoProvider({
  children,
  autoPlay: initialAutoPlay = false,
}: DemoProviderProps) {
  const [scene, setScene] = useState<Scene>('encounter');
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [autoPlay, setAutoPlay] = useState(initialAutoPlay);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const resetDemo = useCallback(() => {
    setScene('encounter');
    setSelectedCaseId(null);
    setAutoPlay(false);
  }, []);

  // Auto-play: cycle through scenes on interval
  useEffect(() => {
    if (!autoPlay) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      return;
    }

    intervalRef.current = setInterval(() => {
      setScene((current) => {
        const idx = SCENE_ORDER.indexOf(current);
        return SCENE_ORDER[(idx + 1) % SCENE_ORDER.length];
      });
    }, AUTO_PLAY_INTERVAL_MS);

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [autoPlay]);

  const value: DemoContextValue = {
    scene,
    setScene,
    selectedCaseId,
    setSelectedCaseId,
    autoPlay,
    setAutoPlay,
    resetDemo,
  };

  return <DemoContext.Provider value={value}>{children}</DemoContext.Provider>;
}
