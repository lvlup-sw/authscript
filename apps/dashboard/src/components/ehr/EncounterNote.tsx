interface Encounter {
  cc: string;
  hpi: string;
  assessment: string;
  plan: string;
}

interface EncounterNoteProps {
  encounter: Encounter;
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <h3 className="mb-1 text-xs font-bold uppercase tracking-wide text-gray-500">
      {children}
    </h3>
  );
}

export function EncounterNote({ encounter }: EncounterNoteProps) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white shadow-sm">
      <div className="border-b border-gray-100 px-5 py-3">
        <h2 className="text-sm font-semibold text-gray-800">
          Encounter Note
        </h2>
      </div>

      <div className="divide-y divide-gray-100 px-5">
        {/* Chief Complaint */}
        <div className="py-3">
          <SectionLabel>Chief Complaint</SectionLabel>
          <p className="text-sm text-gray-700">{encounter.cc}</p>
        </div>

        {/* History of Present Illness */}
        <div className="py-3">
          <SectionLabel>History of Present Illness</SectionLabel>
          <p className="text-sm text-gray-700">{encounter.hpi}</p>
        </div>

        {/* Assessment */}
        <div className="py-3">
          <SectionLabel>Assessment</SectionLabel>
          <p className="text-sm text-gray-700">{encounter.assessment}</p>
        </div>

        {/* Plan */}
        <div className="py-3">
          <SectionLabel>Plan</SectionLabel>
          <p className="text-sm text-gray-700">{encounter.plan}</p>
        </div>
      </div>
    </div>
  );
}
