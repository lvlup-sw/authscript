export interface Encounter {
  cc: string;
  hpi: string;
  assessment: string;
  plan: string;
}

export interface Vitals {
  bp: string;
  hr: number;
  temp: number;
  spo2: number;
}

export interface Order {
  code: string;
  name: string;
  status: 'requires-pa' | 'pending' | 'completed';
}

interface EncounterNoteProps {
  encounter: Encounter;
  vitals?: Vitals;
  orders?: Order[];
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <h3 className="mb-1 text-xs font-bold uppercase tracking-wide text-gray-500">
      {children}
    </h3>
  );
}

function EncounterSection({ label, text }: { label: string; text: string }) {
  return (
    <div>
      <SectionLabel>{label}</SectionLabel>
      <p className="text-sm text-gray-700">{text}</p>
    </div>
  );
}

function VitalsRow({ vitals }: { vitals: Vitals }) {
  return (
    <div className="flex gap-6 border-b border-gray-100 px-5 py-3">
      <div className="text-sm">
        <span className="font-medium text-gray-500">BP</span>{' '}
        <span className="text-gray-700">{vitals.bp}</span>
      </div>
      <div className="text-sm">
        <span className="font-medium text-gray-500">HR</span>{' '}
        <span className="text-gray-700">{vitals.hr}</span>
      </div>
      <div className="text-sm">
        <span className="font-medium text-gray-500">Temp</span>{' '}
        <span className="text-gray-700">{vitals.temp}&deg;F</span>
      </div>
      <div className="text-sm">
        <span className="font-medium text-gray-500">SpO2</span>{' '}
        <span className="text-gray-700">{vitals.spo2}%</span>
      </div>
    </div>
  );
}

const STATUS_CONFIG: Record<Order['status'], { label: string; colors: string }> = {
  'requires-pa': { label: 'Requires PA', colors: 'bg-amber-100 text-amber-800' },
  pending: { label: 'Pending', colors: 'bg-gray-100 text-gray-700' },
  completed: { label: 'Completed', colors: 'bg-green-100 text-green-800' },
};

function StatusBadge({ status }: { status: Order['status'] }) {
  const config = STATUS_CONFIG[status];
  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${config.colors}`}>
      {config.label}
    </span>
  );
}

function OrdersCard({ orders }: { orders: Order[] }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white shadow-sm">
      <div className="border-b border-gray-100 px-5 py-3">
        <h2 className="text-sm font-semibold text-gray-800">Orders</h2>
      </div>
      <div className="divide-y divide-gray-100 px-5">
        {orders.map((order) => (
          <div key={order.code} className="flex items-center gap-3 py-3">
            <span className="inline-flex items-center rounded bg-gray-100 px-2 py-0.5 text-xs font-mono text-gray-600">
              {order.code}
            </span>
            <span className="text-sm text-gray-700">{order.name}</span>
            <StatusBadge status={order.status} />
          </div>
        ))}
      </div>
    </div>
  );
}

export function EncounterNote({
  encounter,
  vitals,
  orders,
}: EncounterNoteProps) {
  const enhanced = !!(vitals || orders);

  return (
    <div className={enhanced ? 'space-y-4' : ''}>
      <div className="rounded-lg border border-gray-200 bg-white shadow-sm">
        <div className="border-b border-gray-100 px-5 py-3">
          <h2 className="text-sm font-semibold text-gray-800">
            Encounter Note
          </h2>
        </div>

        {vitals && <VitalsRow vitals={vitals} />}

        <div className="divide-y divide-gray-100 px-5">
          {enhanced ? (
            <>
              {/* CC/HPI — collapsed by default */}
              <details className="py-3">
                <summary className="cursor-pointer text-xs font-bold uppercase tracking-wide text-gray-500">
                  Chief Complaint / HPI
                </summary>
                <div className="mt-2 space-y-3">
                  <EncounterSection label="Chief Complaint" text={encounter.cc} />
                  <EncounterSection label="History of Present Illness" text={encounter.hpi} />
                </div>
              </details>

              {/* Assessment & Plan — expanded by default */}
              <details open className="py-3">
                <summary className="cursor-pointer text-xs font-bold uppercase tracking-wide text-gray-500">
                  Assessment &amp; Plan
                </summary>
                <div className="mt-2 space-y-3">
                  <EncounterSection label="Assessment" text={encounter.assessment} />
                  <EncounterSection label="Plan" text={encounter.plan} />
                </div>
              </details>
            </>
          ) : (
            <>
              <div className="py-3">
                <EncounterSection label="Chief Complaint" text={encounter.cc} />
              </div>
              <div className="py-3">
                <EncounterSection label="History of Present Illness" text={encounter.hpi} />
              </div>
              <div className="py-3">
                <EncounterSection label="Assessment" text={encounter.assessment} />
              </div>
              <div className="py-3">
                <EncounterSection label="Plan" text={encounter.plan} />
              </div>
            </>
          )}
        </div>
      </div>

      {orders && orders.length > 0 && <OrdersCard orders={orders} />}
    </div>
  );
}
