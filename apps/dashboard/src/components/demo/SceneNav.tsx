import { RotateCcw } from 'lucide-react';
import { useDemoContext } from './DemoProvider';
import type { DemoScene } from './DemoProvider';

const PILLS: { scene: DemoScene; label: string }[] = [
  { scene: 'encounter', label: 'Encounter' },
  { scene: 'fleet', label: 'Fleet' },
  { scene: 'case', label: 'Case Detail' },
];

export function SceneNav() {
  const { scene, setScene, setSelectedCaseId } = useDemoContext();

  function handleReset() {
    setScene('encounter');
    setSelectedCaseId(null);
  }

  return (
    <nav className="flex items-center justify-between px-4 py-1.5 bg-white border-b border-gray-200">
      <div className="flex items-center gap-1">
        {PILLS.map(({ scene: pillScene, label }) => {
          const isActive = scene === pillScene;
          return (
            <button
              key={pillScene}
              type="button"
              data-active={isActive ? 'true' : 'false'}
              onClick={() => setScene(pillScene)}
              className={`px-4 py-1.5 rounded-full text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-teal text-white'
                  : 'text-gray-500 hover:text-gray-700 hover:bg-gray-100 border border-gray-300'
              }`}
            >
              {label}
            </button>
          );
        })}
      </div>

      <button
        type="button"
        onClick={handleReset}
        className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm text-gray-500 hover:text-gray-700 hover:bg-gray-100 transition-colors"
      >
        <RotateCcw className="w-3.5 h-3.5" />
        Reset Demo
      </button>
    </nav>
  );
}
