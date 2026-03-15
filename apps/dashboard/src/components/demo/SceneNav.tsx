import { useContext } from 'react';
import { DemoContext, type Scene } from './DemoProvider';

const SCENES: { key: Scene; label: string }[] = [
  { key: 'encounter', label: 'Encounter' },
  { key: 'fleet', label: 'Fleet' },
  { key: 'case', label: 'Case Detail' },
];

export function SceneNav() {
  const ctx = useContext(DemoContext);
  if (!ctx) return null;

  const { scene, setScene, autoPlay, setAutoPlay, resetDemo } = ctx;

  return (
    <nav
      className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-2"
      aria-label="Demo scene navigation"
      data-testid="scene-nav"
    >
      <div className="flex items-center gap-1">
        {SCENES.map(({ key, label }) => (
          <button
            key={key}
            type="button"
            onClick={() => setScene(key)}
            className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
              scene === key
                ? 'bg-teal-600 text-white'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
            aria-current={scene === key ? 'page' : undefined}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => setAutoPlay(!autoPlay)}
          className={`flex items-center gap-2 rounded-full px-3 py-1.5 text-sm font-medium transition-colors ${
            autoPlay
              ? 'bg-teal-100 text-teal-700'
              : 'text-slate-500 hover:bg-slate-100'
          }`}
          aria-pressed={autoPlay}
        >
          {autoPlay && (
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-teal-500 opacity-75" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-teal-500" />
            </span>
          )}
          Auto-Play
        </button>
        <button
          type="button"
          onClick={resetDemo}
          className="rounded-full px-3 py-1.5 text-sm font-medium text-slate-500 hover:bg-slate-100 transition-colors"
        >
          Reset Demo
        </button>
      </div>
    </nav>
  );
}
