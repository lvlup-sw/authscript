import { createFileRoute } from '@tanstack/react-router';
import { DemoProvider } from '@/components/demo/DemoProvider';
import { SceneNav } from '@/components/demo/SceneNav';

export function CaseDetailPage() {
  const { caseId } = Route.useParams();

  return (
    <DemoProvider>
      <div className="min-h-screen bg-gray-50">
        <SceneNav />
        <div className="max-w-7xl mx-auto p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-6">Case Detail</h1>
          <p className="text-gray-500">
            Case <span className="font-mono">{caseId}</span> — pending implementation of case components.
          </p>
        </div>
      </div>
    </DemoProvider>
  );
}

export const Route = createFileRoute('/case/$caseId')({
  component: CaseDetailPage,
});
