import { useState, useMemo } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { Activity as ActivityIcon } from 'lucide-react';
import { SceneNav } from '@/components/demo/SceneNav';
import { usePARequests, usePAStats, useActivity, type ActivityItem } from '@/api/graphqlService';
import { generateFleetData, type FleetPARequest, type FleetStatus } from '@/lib/fleetSeedData';
import { KPICards, type KPIStats } from '@/components/fleet/KPICards';
import { CasePipeline } from '@/components/fleet/CasePipeline';
import { FleetView } from '@/components/fleet/FleetView';

export const Route = createFileRoute('/fleet')({
  component: FleetPage,
});

/** Compute KPI stats from fleet request data */
function computeStats(requests: FleetPARequest[]): KPIStats {
  const counts: KPIStats = {
    total: requests.length,
    processing: 0,
    ready: 0,
    submitted: 0,
    approved: 0,
    denied: 0,
  };

  for (const r of requests) {
    if (r.status in counts && r.status !== 'waiting_for_insurance') {
      counts[r.status as Exclude<FleetStatus, 'waiting_for_insurance'>] += 1;
    }
  }

  return counts;
}

/** Compute pipeline stage counts */
function computePipelineCounts(requests: FleetPARequest[]): Record<string, number> {
  const total = requests.length;
  const processing = requests.filter((r) => r.status === 'processing').length;
  const ready = requests.filter((r) => r.status === 'ready').length;
  const submitted = requests.filter((r) => r.status === 'submitted').length;
  const payerResponse = requests.filter(
    (r) => r.status === 'waiting_for_insurance' || r.status === 'approved' || r.status === 'denied',
  ).length;

  return {
    order_signed: total,
    pa_detected: total - processing,
    processing,
    ready,
    submitted,
    payer_response: payerResponse,
  };
}

function ActivityFeedItem({ item }: { item: ActivityItem }) {
  const colors = {
    success: 'bg-green-500/10 text-green-700 border-green-500/20',
    ready: 'bg-teal-500/10 text-teal-700 border-teal-500/20',
    info: 'bg-muted text-muted-foreground border-border',
  };

  return (
    <div className={`flex items-center gap-3 p-3 rounded-lg border ${colors[item.type]}`}>
      <div className="w-2 h-2 rounded-full bg-current shrink-0" />
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium truncate">{item.action}</p>
        <p className="text-xs opacity-70 truncate">
          {item.patientName} &bull; {item.procedureCode}
        </p>
      </div>
      <span className="text-xs opacity-70 shrink-0">{item.time}</span>
    </div>
  );
}

export function FleetPage() {
  const [activeFilter, setActiveFilter] = useState<string | null>(null);

  // GraphQL data (may not be available)
  const { data: graphqlRequests } = usePARequests();
  const { data: _graphqlStats } = usePAStats();
  const { data: activity = [] } = useActivity();

  // Fleet data: use GraphQL requests if available, otherwise generate seed data
  const fleetData = useMemo<FleetPARequest[]>(() => {
    if (graphqlRequests && graphqlRequests.length > 0) {
      // Map GraphQL PARequests to FleetPARequests (status mapping)
      return graphqlRequests.map((r) => ({
        ...r,
        status: r.status === 'draft' ? ('processing' as FleetStatus) : (r.status as FleetStatus),
      }));
    }
    return generateFleetData();
  }, [graphqlRequests]);

  const stats = useMemo(() => computeStats(fleetData), [fleetData]);
  const pipelineCounts = useMemo(
    () => computePipelineCounts(fleetData),
    [fleetData],
  );

  const handleFilter = (status: string) => {
    setActiveFilter((prev) => (prev === status ? null : status));
  };

  const handlePipelineFilter = (stage: string) => {
    // Map pipeline stages to fleet filter statuses
    const stageToStatus: Record<string, string | null> = {
      order_signed: null,
      pa_detected: null,
      processing: 'processing',
      ready: 'ready',
      submitted: 'submitted',
      payer_response: null,
    };
    const status = stageToStatus[stage];
    if (status) {
      setActiveFilter((prev) => (prev === status ? null : status));
    }
  };

  // KPI total filter shows all
  const handleKPIFilter = (key: string) => {
    if (key === 'total') {
      setActiveFilter(null);
    } else {
      handleFilter(key);
    }
  };

  return (
    <div className="p-6 space-y-6">
      <SceneNav />
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">
            Prior Authorization Command Center
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Real-time fleet view of all PA requests across your practice
          </p>
        </div>
        <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-green-500/10 text-green-700 text-sm font-medium">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-500 opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500" />
          </span>
          Live
        </div>
      </div>

      {/* KPI Cards */}
      <KPICards
        stats={stats}
        activeFilter={activeFilter}
        onFilter={handleKPIFilter}
      />

      {/* Case Pipeline */}
      <div className="bg-card rounded-xl border shadow-sm p-4">
        <CasePipeline
          stageCounts={pipelineCounts}
          activeStage={activeFilter}
          onFilter={handlePipelineFilter}
        />
      </div>

      {/* Fleet View + Activity Sidebar */}
      <div className="grid grid-cols-4 gap-6">
        {/* Fleet Grid - 3 cols */}
        <div className="col-span-3">
          <div className="bg-card rounded-xl border shadow-sm p-4">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-bold text-foreground">
                Active Cases
                {activeFilter && (
                  <span className="ml-2 text-sm font-normal text-muted-foreground">
                    Filtered: {activeFilter.replace(/_/g, ' ')}
                  </span>
                )}
              </h2>
              <span className="text-sm text-muted-foreground">
                {activeFilter
                  ? `${fleetData.filter((r) => r.status === activeFilter).length} of ${fleetData.length}`
                  : `${fleetData.length} total`}
              </span>
            </div>
            <FleetView
              requests={fleetData}
              filter={activeFilter}
              onSelectCase={() => {
                // Will be wired to case detail navigation in future task
              }}
            />
          </div>
        </div>

        {/* Activity Feed Sidebar - 1 col */}
        <div className="col-span-1">
          <div className="bg-card rounded-xl border shadow-sm p-4">
            <div className="flex items-center gap-2 mb-4">
              <ActivityIcon className="w-4 h-4 text-muted-foreground" />
              <h3 className="font-bold text-foreground">Recent Activity</h3>
            </div>
            <div className="space-y-2">
              {activity.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-4">
                  No recent activity
                </p>
              ) : (
                activity.map((item) => (
                  <ActivityFeedItem key={item.id} item={item} />
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
