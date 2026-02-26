interface EhrHeaderPatient {
  name: string;
  dob: string;
  mrn: string;
}

interface EncounterMeta {
  provider: string;
  specialty: string;
  date: string;
  type: string;
}

interface EhrHeaderProps {
  patient: EhrHeaderPatient;
  encounterMeta?: EncounterMeta;
}

const NAV_ITEMS = ['Charts', 'Schedule', 'Messages', 'Admin'] as const;

export function EhrHeader({ patient, encounterMeta }: EhrHeaderProps) {
  return (
    <header>
      <div className="bg-[#1a365d] text-white">
        <div className="flex items-center justify-between px-4 py-2">
          <span className="text-lg font-bold tracking-wide">athenaOne</span>
          <nav className="flex gap-1">
            {NAV_ITEMS.map((item) => (
              <button
                key={item}
                type="button"
                className="rounded px-3 py-1.5 text-sm font-medium text-white/80 transition-colors hover:bg-white/10 hover:text-white"
              >
                {item}
              </button>
            ))}
          </nav>
        </div>
      </div>
      <div className="border-b border-gray-200 bg-gray-50 px-4 py-3">
        <div className="flex items-center gap-6">
          <span className="text-base font-bold text-gray-900">{patient.name}</span>
          <div className="flex items-center gap-4 text-sm text-gray-600">
            <span>
              <span className="font-medium text-gray-500">DOB:</span> {patient.dob}
            </span>
            <span>
              <span className="font-medium text-gray-500">MRN:</span> {patient.mrn}
            </span>
          </div>
        </div>
      </div>
      {encounterMeta && (
        <div className="border-b border-gray-200 bg-white px-4 py-2">
          <div className="flex items-center gap-3 text-xs text-gray-500">
            <span className="font-medium text-gray-700">{encounterMeta.provider}</span>
            <span>&middot;</span>
            <span>{encounterMeta.specialty}</span>
            <span>&middot;</span>
            <span>{encounterMeta.date}</span>
            <span>&middot;</span>
            <span>{encounterMeta.type}</span>
          </div>
        </div>
      )}
    </header>
  );
}
