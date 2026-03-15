import { createFileRoute } from '@tanstack/react-router';
import { DemoProvider } from '@/components/demo/DemoProvider';
import { SceneNav } from '@/components/demo/SceneNav';

export const Route = createFileRoute('/fleet')({
  component: FleetPage,
});

function FleetPage() {
  return (
    <DemoProvider>
      <SceneNav />
      <div className="p-6">
        <h1 className="text-2xl font-bold text-gray-900">
          Prior Authorization Command Center
        </h1>
      </div>
    </DemoProvider>
  );
}
