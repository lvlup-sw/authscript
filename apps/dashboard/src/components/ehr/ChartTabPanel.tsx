import type { DEMO_CHART_DATA } from '@/lib/demoData';

type ChartData = typeof DEMO_CHART_DATA;

interface ChartTabPanelProps {
  activeTab: string;
  chartData: ChartData;
}

function ProblemsPanel({ problems }: { problems: ChartData['problems'] }) {
  return (
    <ul className="space-y-1.5">
      {problems.map((problem) => (
        <li key={problem.code} className="text-xs">
          <span className="font-mono font-medium text-gray-700">{problem.code}</span>
          <span className="ml-1.5 text-gray-500">{problem.description}</span>
        </li>
      ))}
    </ul>
  );
}

function MedicationsPanel({ medications }: { medications: ChartData['medications'] }) {
  return (
    <ul className="space-y-1.5">
      {medications.map((med) => (
        <li key={med.name} className="text-xs">
          <span className="font-medium text-gray-700">{med.name}</span>
          <span className="ml-1 text-gray-400">
            {med.dosage} {med.frequency}
          </span>
        </li>
      ))}
    </ul>
  );
}

function AllergiesPanel({ allergies }: { allergies: ChartData['allergies'] }) {
  return (
    <div className="text-xs text-gray-500">
      {allergies === 'NKDA' ? 'No Known Drug Allergies' : allergies}
    </div>
  );
}

function ImagingPanel({ imagingHistory }: { imagingHistory: ChartData['imagingHistory'] }) {
  if (imagingHistory.length === 0) {
    return <div className="text-xs text-gray-400 italic">No prior lumbar imaging</div>;
  }

  return (
    <ul className="space-y-1.5">
      {imagingHistory.map((item) => (
        <li key={`${item.date}-${item.type}`} className="text-xs">
          <span className="font-medium text-gray-700">{item.type}</span>
          <span className="ml-1 text-gray-400">{item.date}</span>
          <span className="block text-gray-500">{item.result}</span>
        </li>
      ))}
    </ul>
  );
}

function VitalsPanel() {
  return (
    <div className="space-y-1 text-xs text-gray-500">
      <div><span className="font-medium text-gray-600">BP:</span> 128/82</div>
      <div><span className="font-medium text-gray-600">HR:</span> 72</div>
      <div><span className="font-medium text-gray-600">Temp:</span> 98.6&deg;F</div>
      <div><span className="font-medium text-gray-600">SpO2:</span> 99%</div>
    </div>
  );
}

function LabsPanel({ labResults }: { labResults: ChartData['labResults'] }) {
  return (
    <ul className="space-y-1.5">
      {labResults.map((lab) => (
        <li key={lab.name} className="text-xs">
          <span className="font-medium text-gray-700">{lab.name}</span>
          <span className="ml-1 text-gray-500">{lab.value}</span>
          <span className="block text-gray-400">{lab.date}</span>
        </li>
      ))}
    </ul>
  );
}

export function ChartTabPanel({ activeTab, chartData }: ChartTabPanelProps) {
  return (
    <div className="px-3 py-2">
      {activeTab === 'problems' && <ProblemsPanel problems={chartData.problems} />}
      {activeTab === 'medications' && <MedicationsPanel medications={chartData.medications} />}
      {activeTab === 'allergies' && <AllergiesPanel allergies={chartData.allergies} />}
      {activeTab === 'imaging' && <ImagingPanel imagingHistory={chartData.imagingHistory} />}
      {activeTab === 'vitals' && <VitalsPanel />}
      {activeTab === 'labs' && <LabsPanel labResults={chartData.labResults} />}
    </div>
  );
}
