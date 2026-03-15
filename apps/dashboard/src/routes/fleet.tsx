import { createFileRoute } from '@tanstack/react-router';
import { DemoProvider } from '@/components/demo/DemoProvider';
import { SceneNav } from '@/components/demo/SceneNav';

export function FleetPage() {
  return (
    <DemoProvider>
      <div className="min-h-screen bg-gray-50">
        <SceneNav />
        <div className="max-w-7xl mx-auto p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-6">Fleet Dashboard</h1>
          <p className="text-gray-500">Fleet view — pending implementation of fleet components.</p>
        </div>
      </div>
    </DemoProvider>
  );
}

export const Route = createFileRoute('/fleet')({
  component: FleetPage,
});
