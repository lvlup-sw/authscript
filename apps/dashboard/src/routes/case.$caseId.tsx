import { createFileRoute } from '@tanstack/react-router';
import { DemoProvider } from '@/components/demo/DemoProvider';
import { SceneNav } from '@/components/demo/SceneNav';

export const Route = createFileRoute('/case/$caseId')({
  component: CaseDetailPage,
});

function CaseDetailPage() {
  const { caseId } = Route.useParams();

  return (
    <DemoProvider>
      <SceneNav />
      <div className="p-6">
        <h1 className="text-2xl font-bold text-gray-900">
          Case Detail
        </h1>
        <p className="text-sm text-gray-500 mt-1">Case ID: {caseId}</p>
      </div>
    </DemoProvider>
  );
}
