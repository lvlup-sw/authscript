import { useState, useEffect } from 'react';
import { createFileRoute, Link } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import {
  CheckCircle2,
  AlertTriangle,
  Sparkles,
  FileText,
  Brain,
  Bell,
  Send,
  TrendingUp,
  Activity,
  ChevronRight,
  Calendar,
  ClipboardCheck,
  Zap,
  BarChart3,
  Building2,
  Check,
  Plus,
  Timer,
  Hourglass,
} from 'lucide-react';
import { usePARequests, usePAStats, useActivity, type PARequest, type ActivityItem } from '@/api/graphqlService';
import { NewPAModal } from '@/components/NewPAModal';
import { Skeleton, SkeletonRow, SkeletonStats } from '@/components/LoadingSpinner';

export const Route = createFileRoute('/')({
  component: DashboardPage,
});

// Stat Card
function StatCard({ 
  label, 
  value, 
  change, 
  positive, 
  icon: Icon
}: { 
  label: string; 
  value: string | number; 
  change?: string; 
  positive?: boolean;
  icon: React.ElementType;
}) {
  return (
    <div className="rounded-2xl p-5 bg-card border shadow-soft text-center">
      <div className="flex justify-center mb-3">
        <div className="w-10 h-10 rounded-xl flex items-center justify-center bg-teal-tint">
          <Icon className="w-5 h-5 text-teal" />
        </div>
      </div>
      <p className="text-3xl font-bold text-foreground">{value}</p>
      <p className="text-sm text-muted-foreground mt-1">{label}</p>
      {change && (
        <span className={cn(
          'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold mt-2',
          positive ? 'bg-success/10 text-success' : 'bg-destructive/10 text-destructive'
        )}>
          <TrendingUp className={cn('w-3 h-3', !positive && 'rotate-180')} />
          {change}
        </span>
      )}
    </div>
  );
}

// Horizontal Workflow Pipeline
function WorkflowPipeline() {
  const steps = [
    { icon: FileText, label: 'Signed', completed: true },
    { icon: Activity, label: 'Detected', completed: true },
    { icon: Brain, label: 'Processing', active: true },
    { icon: Bell, label: 'Ready', completed: false },
    { icon: Send, label: 'Submitted', completed: false },
  ];

  return (
    <div className="flex items-center">
      {steps.map((step, index) => {
        const Icon = step.icon;
        const isCompleted = step.completed;
        const isActive = step.active;
        
        return (
          <div key={step.label} className="flex items-center flex-1 last:flex-none">
            {/* Circle and Label */}
            <div className="flex flex-col items-center">
              <div className={cn(
                'w-12 h-12 rounded-full flex items-center justify-center transition-all border-2',
                isCompleted && 'bg-teal border-teal text-white',
                isActive && 'bg-teal/10 border-teal text-teal',
                !isCompleted && !isActive && 'bg-card border-gray-200 text-gray-400'
              )}>
                {isCompleted ? <Check className="w-5 h-5" /> : <Icon className="w-5 h-5" />}
              </div>
              <span className={cn(
                'text-xs font-medium mt-2 whitespace-nowrap',
                isCompleted && 'text-teal',
                isActive && 'text-teal font-semibold',
                !isCompleted && !isActive && 'text-gray-400'
              )}>
                {step.label}
              </span>
            </div>
            
            {/* Connecting Line (after circle, except for last) */}
            {index < steps.length - 1 && (
              <div className={cn(
                'h-0.5 flex-1 mx-2',
                (isCompleted || isActive) ? 'bg-teal' : 'bg-gray-200'
              )} />
            )}
          </div>
        );
      })}
    </div>
  );
}

// Low confidence threshold: same as "Needs Attention" (ready + confidence < 70)
const LOW_CONFIDENCE_THRESHOLD = 70;

// Time range presets for analytics
type TimeRangePreset = 'day' | 'week' | 'month' | 'year' | 'custom';

function getTimeRangeBounds(preset: TimeRangePreset, customStart?: Date, customEnd?: Date): { start: Date; end: Date } {
  const now = new Date();
  const end = preset === 'custom' && customEnd ? customEnd : now;
  const start = (() => {
    if (preset === 'custom' && customStart) return customStart;
    const d = new Date(end);
    if (preset === 'day') d.setDate(d.getDate() - 1);
    else if (preset === 'week') d.setDate(d.getDate() - 7);
    else if (preset === 'month') d.setMonth(d.getMonth() - 1);
    else if (preset === 'year') d.setFullYear(d.getFullYear() - 1);
    return d;
  })();
  return { start, end };
}

function formatDuration(ms: number): string {
  if (ms < 60000) return `${Math.round(ms / 1000)}s`;
  const mins = Math.floor(ms / 60000);
  const secs = Math.round((ms % 60000) / 1000);
  return secs > 0 ? `${mins}m ${secs}s` : `${mins}m`;
}

// Analytics view
function AnalyticsView({
  requests,
  timeRange,
  onTimeRangeChange,
  customStart,
  customEnd,
  onCustomRangeChange,
}: {
  requests: PARequest[];
  timeRange: TimeRangePreset;
  onTimeRangeChange: (preset: TimeRangePreset) => void;
  customStart?: Date;
  customEnd?: Date;
  onCustomRangeChange?: (start: Date, end: Date) => void;
}) {
  const { start, end } = getTimeRangeBounds(timeRange, customStart, customEnd);

  const submittedRequests = requests.filter(
    r =>
      (r.status === 'waiting_for_insurance' || r.status === 'approved' || r.status === 'denied') &&
      new Date(r.updatedAt) >= start &&
      new Date(r.updatedAt) <= end
  );

  const processingTimes = submittedRequests.map(r => {
    const created = new Date(r.createdAt).getTime();
    const ready = r.readyAt ? new Date(r.readyAt).getTime() : new Date(r.updatedAt).getTime();
    const timeToReadyMs = ready - created;
    const reviewMs = (r.reviewTimeSeconds ?? 0) * 1000;
    const totalMs = timeToReadyMs + reviewMs;
    return { request: r, ms: totalMs };
  });

  const totalProcessed = submittedRequests.length;
  const avgProcessingMs =
    processingTimes.length > 0
      ? processingTimes.reduce((sum, t) => sum + t.ms, 0) / processingTimes.length
      : 0;
  const avgConfidence =
    submittedRequests.length > 0
      ? submittedRequests.reduce((sum, r) => sum + r.confidence, 0) / submittedRequests.length
      : 0;
  const approvedCount = submittedRequests.filter(r => r.status === 'approved').length;
  const deniedCount = submittedRequests.filter(r => r.status === 'denied').length;
  const withOutcome = approvedCount + deniedCount;
  const successRate = withOutcome > 0 ? (approvedCount / withOutcome) * 100 : 0;

  const longest = processingTimes.length > 0 ? processingTimes.reduce((a, b) => (a.ms >= b.ms ? a : b)) : null;
  const shortest = processingTimes.length > 0 ? processingTimes.reduce((a, b) => (a.ms <= b.ms ? a : b)) : null;

  return (
    <div className="space-y-6">
      {/* Time range selector */}
      <div className="flex flex-wrap items-center gap-3">
        <span className="text-sm font-medium text-muted-foreground">Time range:</span>
        {(['day', 'week', 'month', 'year'] as const).map(preset => (
          <button
            key={preset}
            onClick={() => onTimeRangeChange(preset)}
            className={cn(
              'px-3 py-1.5 rounded-lg text-sm font-medium transition-colors click-effect',
              timeRange === preset
                ? 'bg-teal text-white'
                : 'bg-secondary text-muted-foreground hover:bg-secondary/80 hover:text-foreground'
            )}
          >
            {preset.charAt(0).toUpperCase() + preset.slice(1)}
          </button>
        ))}
        <button
          onClick={() => onTimeRangeChange('custom')}
          className={cn(
            'px-3 py-1.5 rounded-lg text-sm font-medium transition-colors click-effect',
            timeRange === 'custom'
              ? 'bg-teal text-white'
              : 'bg-secondary text-muted-foreground hover:bg-secondary/80 hover:text-foreground'
          )}
        >
          Custom
        </button>
        {timeRange === 'custom' && (
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={customStart?.toISOString().slice(0, 10) ?? ''}
              onChange={e => {
                const d = new Date(e.target.value);
                onCustomRangeChange?.(d, customEnd ?? new Date());
              }}
              className="px-2 py-1.5 rounded-lg border text-sm"
            />
            <span className="text-muted-foreground">–</span>
            <input
              type="date"
              value={customEnd?.toISOString().slice(0, 10) ?? ''}
              onChange={e => {
                const d = new Date(e.target.value);
                onCustomRangeChange?.(customStart ?? new Date(), d);
              }}
              className="px-2 py-1.5 rounded-lg border text-sm"
            />
          </div>
        )}
        <span className="text-xs text-muted-foreground ml-2">
          {start.toLocaleDateString()} – {end.toLocaleDateString()}
        </span>
      </div>

      {/* Metrics grid */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="rounded-2xl p-5 bg-card border shadow-soft">
          <p className="text-2xl font-bold text-foreground">{totalProcessed}</p>
          <p className="text-sm text-muted-foreground mt-1">Total Processed</p>
        </div>
        <div className="rounded-2xl p-5 bg-card border shadow-soft">
          <p className="text-2xl font-bold text-foreground">{formatDuration(avgProcessingMs)}</p>
          <p className="text-sm text-muted-foreground mt-1">Avg. Processing Time</p>
        </div>
        <div className="rounded-2xl p-5 bg-card border shadow-soft">
          <p className="text-2xl font-bold text-teal">{Math.round(avgConfidence)}%</p>
          <p className="text-sm text-muted-foreground mt-1">AI Confidence Avg</p>
        </div>
        <div className="rounded-2xl p-5 bg-card border shadow-soft">
          <p className="text-2xl font-bold text-success">{withOutcome > 0 ? `${Math.round(successRate)}%` : '—'}</p>
          <p className="text-sm text-muted-foreground mt-1">Success Rate</p>
        </div>
      </div>

      {/* Longest / Shortest */}
      <div className="grid grid-cols-2 gap-4">
        <div className="rounded-2xl p-5 bg-card border shadow-soft">
          <div className="flex items-center gap-2 mb-3">
            <Timer className="w-5 h-5 text-warning" />
            <h3 className="font-bold text-foreground">Longest Time to Submit</h3>
          </div>
          {longest ? (
            <div>
              <p className="text-xl font-bold text-foreground">{formatDuration(longest.ms)}</p>
              <p className="text-sm text-muted-foreground mt-1">
                {longest.request.patient.name} • {longest.request.procedureCode}
              </p>
              <Link
                to="/analysis/$transactionId"
                params={{ transactionId: longest.request.id }}
                className="text-xs text-teal hover:underline mt-2 inline-block"
              >
                View details <ChevronRight className="w-3 h-3 inline" />
              </Link>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">No submitted requests in range</p>
          )}
        </div>
        <div className="rounded-2xl p-5 bg-card border shadow-soft">
          <div className="flex items-center gap-2 mb-3">
            <Zap className="w-5 h-5 text-success" />
            <h3 className="font-bold text-foreground">Shortest Time to Submit</h3>
          </div>
          {shortest ? (
            <div>
              <p className="text-xl font-bold text-foreground">{formatDuration(shortest.ms)}</p>
              <p className="text-sm text-muted-foreground mt-1">
                {shortest.request.patient.name} • {shortest.request.procedureCode}
              </p>
              <Link
                to="/analysis/$transactionId"
                params={{ transactionId: shortest.request.id }}
                className="text-xs text-teal hover:underline mt-2 inline-block"
              >
                View details <ChevronRight className="w-3 h-3 inline" />
              </Link>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">No submitted requests in range</p>
          )}
        </div>
      </div>
    </div>
  );
}

// Request Row
function RequestRow({ request, index = 0, now }: { request: PARequest; index?: number; now: number }) {
  const isReady = request.status === 'ready';
  const lowConfidence = isReady && request.confidence < LOW_CONFIDENCE_THRESHOLD;
  
  const displayConf = Math.max(1, request.confidence); // Never show 0%
  const confidenceColor = displayConf >= 80 ? 'text-success bg-success/10' : 
                          displayConf >= 60 ? 'text-warning bg-warning/10' : 
                          'text-destructive bg-destructive/10';
  
  const getTimeAgo = (dateString: string) => {
    const diff = now - new Date(dateString).getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    return `${Math.floor(hours / 24)}d ago`;
  };

  return (
    <div 
      className={cn(
        'flex items-center gap-4 p-4 rounded-xl transition-all cursor-pointer group hover-lift animate-fade-in click-effect-card',
        isReady ? 'bg-teal/5 hover:bg-teal/10 border border-teal/20' : 'hover:bg-secondary/50 border border-transparent',
        lowConfidence && 'border-l-4 border-l-amber-500 bg-amber-50/50 hover:bg-amber-50/70'
      )}
      style={{ animationDelay: `${index * 0.05}s` }}
    >
      {/* Avatar */}
      <div className={cn(
        'w-11 h-11 rounded-xl flex items-center justify-center font-bold flex-shrink-0',
        isReady ? 'bg-teal text-white' : 'bg-secondary text-muted-foreground'
      )}>
        {request.patient.name.split(' ').map((n: string) => n[0]).join('')}
      </div>
      
      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <p className="font-semibold text-foreground">{request.patient.name}</p>
          {lowConfidence && (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 bg-amber-100 text-amber-800 text-[10px] font-bold rounded-full border border-amber-300">
              <AlertTriangle className="w-3 h-3 flex-shrink-0" />
              Low confidence
            </span>
          )}
        </div>
        <p className="text-sm text-muted-foreground">
          <span className="font-medium text-foreground/70">{request.procedureCode}</span>
          <span className="mx-2">•</span>
          {request.payer}
          <span className="mx-2">•</span>
          {getTimeAgo(request.createdAt)}
        </p>
      </div>
      
      {/* Status & Confidence */}
      <div className="flex items-center gap-3">
        <span className={cn('px-3 py-1 rounded-lg text-sm font-bold', confidenceColor)}>
          {displayConf}%
        </span>
        <span className={cn(
          'px-3 py-1.5 rounded-lg text-xs font-semibold',
          isReady ? 'bg-teal/10 text-teal' : 'bg-secondary text-muted-foreground'
        )}>
          {request.status === 'ready'
            ? 'Ready'
            : request.status === 'waiting_for_insurance'
                ? 'Waiting for insurance'
                : request.status === 'approved'
                  ? 'Approved'
                  : request.status === 'denied'
                    ? 'Denied'
                    : request.status}
        </span>
      </div>
      
      {/* Action */}
      <Link
        to="/analysis/$transactionId"
        params={{ transactionId: request.id }}
        className={cn(
          'px-4 py-2 rounded-xl text-sm font-semibold transition-all flex items-center gap-2 click-effect-primary',
          isReady 
            ? 'bg-teal text-white hover:bg-teal/90 shadow-sm' 
            : 'bg-secondary text-foreground hover:bg-secondary/80'
        )}
      >
        {isReady ? 'Review' : 'View'}
        <ChevronRight className="w-4 h-4" />
      </Link>
    </div>
  );
}

// Activity Item
function ActivityItemComponent({ item }: { item: ActivityItem }) {
  const colors = {
    success: 'bg-success/10 text-success border-success/20',
    ready: 'bg-teal/10 text-teal border-teal/20',
    info: 'bg-muted text-muted-foreground border-border',
  };
  
  return (
    <div className={cn('flex items-center gap-3 p-3 rounded-xl border', colors[item.type])}>
      <div className="w-2 h-2 rounded-full bg-current flex-shrink-0" />
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium">{item.action}</p>
        <p className="text-xs opacity-70">{item.patientName} • {item.procedureCode}</p>
      </div>
      <span className="text-xs opacity-70">{item.time}</span>
    </div>
  );
}

function DashboardPage() {
  const [activeTab, setActiveTab] = useState('pending');
  const [isNewPAModalOpen, setIsNewPAModalOpen] = useState(false);
  const [now, setNow] = useState(() => Date.now());
  const [analyticsTimeRange, setAnalyticsTimeRange] = useState<TimeRangePreset>('week');
  const [analyticsCustomStart, setAnalyticsCustomStart] = useState<Date>(() => {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d;
  });
  const [analyticsCustomEnd, setAnalyticsCustomEnd] = useState<Date>(() => new Date());

  const { data: requests = [], isLoading, isError, error } = usePARequests();
  const { data: stats = { ready: 0, submitted: 0, waitingForInsurance: 0, attention: 0, total: 0 } } = usePAStats();
  const { data: activity = [] } = useActivity();

  // Update "now" every minute for relative time display (avoids impure Date.now() during render)
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 60000);
    return () => clearInterval(id);
  }, []);

  const currentDate = new Date().toLocaleDateString('en-US', { 
    weekday: 'long', 
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  });

  const tabs = [
    { id: 'pending', label: 'Pending Review', icon: ClipboardCheck, count: stats.ready },
    { id: 'waiting', label: 'Waiting for Insurance', icon: Hourglass, count: stats.waitingForInsurance },
    { id: 'history', label: 'History', icon: FileText },
    { id: 'analytics', label: 'Analytics', icon: BarChart3 },
  ];

  const filteredRequests = requests.filter(r => {
    if (activeTab === 'pending') return r.status === 'ready';
    if (activeTab === 'waiting') return r.status === 'waiting_for_insurance';
    if (activeTab === 'history') return r.status === 'approved' || r.status === 'denied';
    return true;
  });

  return (
    <div className="p-6 space-y-6 animate-fade-in">
      {/* GraphQL connection error */}
      {isError && (
        <div className="rounded-2xl p-4 bg-destructive/10 border border-destructive/30 text-destructive">
          <p className="font-semibold">Cannot connect to backend</p>
          <p className="text-sm mt-1 opacity-90">
            {error instanceof Error ? error.message : 'GraphQL request failed'}
          </p>
          <p className="text-xs mt-2 opacity-75">
            Ensure the Gateway is running. Set <code className="bg-black/10 px-1 rounded">VITE_GATEWAY_URL</code> to your
            Gateway URL (default: <code className="bg-black/10 px-1 rounded">http://localhost:5000</code>).
          </p>
        </div>
      )}

      {/* Welcome Header Banner */}
      <div className="bg-gradient-to-r from-slate-800 via-slate-700 to-slate-800 rounded-2xl p-6 text-white relative overflow-hidden">
        {/* Background decoration */}
        <div className="absolute top-0 right-0 w-64 h-64 bg-teal/20 rounded-full blur-3xl -translate-y-1/2 translate-x-1/4" />
        <div className="absolute bottom-0 left-1/3 w-48 h-48 bg-white/5 rounded-full blur-2xl translate-y-1/2" />
        
        <div className="relative flex items-center justify-between">
          <div className="flex items-center gap-5">
            {/* Avatar */}
            <div className="w-16 h-16 rounded-2xl bg-teal flex items-center justify-center text-white text-xl font-bold shadow-teal">
              FC
            </div>
            
            {/* Info */}
            <div>
              <p className="text-white/60 text-sm mb-1">Welcome back</p>
              <h1 className="text-2xl font-bold">Family Care Associates</h1>
              <div className="flex items-center gap-4 mt-2 text-sm">
                <span className="flex items-center gap-2 text-white/90">
                  <Building2 className="w-4 h-4 text-teal" />
                  athenahealth Connected
                </span>
                <span className="flex items-center gap-2 text-white/60">
                  <Calendar className="w-4 h-4" />
                  {currentDate}
                </span>
              </div>
            </div>
          </div>
          
          <div className="flex items-center gap-3">
            {/* New PA Button */}
            <button
              onClick={() => setIsNewPAModalOpen(true)}
              className="flex items-center gap-2 px-4 py-2.5 bg-teal text-white rounded-xl font-semibold hover:bg-teal/90 transition-colors shadow-teal click-effect-primary"
            >
              <Plus className="w-5 h-5" />
              New PA Request
            </button>
            
            {/* Live Status */}
            <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/10 text-sm">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-success opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-success"></span>
              </span>
              <span>Monitoring Active</span>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Row */}
      {isLoading ? (
        <SkeletonStats />
      ) : (
        <div className="grid grid-cols-3 gap-4">
          <StatCard 
            label="Ready for Review" 
            value={stats.ready} 
            change={stats.ready > 0 ? `${stats.ready} pending` : undefined}
            positive 
            icon={CheckCircle2}
          />
          <StatCard 
            label="Completed Today" 
            value={stats.submitted} 
            change={stats.submitted > 0 ? "+18% vs avg" : undefined}
            positive 
            icon={Sparkles}
          />
          <StatCard 
            label="Needs Attention" 
            value={stats.attention}
            change={stats.attention > 0 ? "Low confidence" : undefined}
            icon={AlertTriangle}
          />
        </div>
      )}

      {/* Pipeline Status - Horizontal Circles */}
      <div className="bg-card rounded-2xl border shadow-soft p-6">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-lg font-bold text-foreground">Current Pipeline Status</h2>
            <p className="text-sm text-muted-foreground">Real-time view of PA requests being processed</p>
          </div>
          <div className="flex items-center gap-4 text-sm">
            <span className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-success/10 text-success font-medium">
              <CheckCircle2 className="w-4 h-4" />
              {stats.submitted} completed today
            </span>
          </div>
        </div>
        
        {/* Horizontal Workflow */}
        <div className="px-8">
          <WorkflowPipeline />
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="border-b">
        <div className="flex gap-1">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                'flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 transition-colors click-effect',
                activeTab === tab.id 
                  ? 'border-teal text-teal' 
                  : 'border-transparent text-muted-foreground hover:text-foreground hover:border-border'
              )}
            >
              <tab.icon className="w-4 h-4" />
              {tab.label}
              {tab.count !== undefined && (
                <span className={cn(
                  'px-2 py-0.5 rounded-full text-xs font-bold',
                  activeTab === tab.id ? 'bg-teal/10 text-teal' : 'bg-secondary text-muted-foreground'
                )}>
                  {tab.count}
                </span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Main Content Grid */}
      <div className={cn('grid gap-6', activeTab === 'analytics' ? 'grid-cols-1' : 'grid-cols-3')}>
        {/* Requests list or Analytics */}
        <div className={activeTab === 'analytics' ? 'w-full' : 'col-span-2'}>
          <div className="bg-card rounded-2xl border shadow-soft">
            <div className="p-5 border-b flex items-center justify-between">
              <div>
              <h2 className="text-lg font-bold text-foreground">
                {activeTab === 'pending' && 'Pending Requests'}
                {activeTab === 'waiting' && 'Waiting for Insurance'}
                  {activeTab === 'history' && 'History'}
                  {activeTab === 'analytics' && 'Analytics'}
                </h2>
                <p className="text-sm text-muted-foreground">
                  {activeTab === 'analytics' ? 'Performance metrics by time range' : `${filteredRequests.length} requests`}
                </p>
              </div>
              {activeTab !== 'analytics' && (
                <button
                  onClick={() => setIsNewPAModalOpen(true)}
                  className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-teal hover:bg-teal/10 rounded-lg transition-colors click-effect"
                >
                  <Plus className="w-4 h-4" />
                  New Request
                </button>
              )}
            </div>
            <div className="p-4 space-y-3">
              {activeTab === 'analytics' ? (
                <AnalyticsView
                  requests={requests}
                  timeRange={analyticsTimeRange}
                  onTimeRangeChange={setAnalyticsTimeRange}
                  customStart={analyticsTimeRange === 'custom' ? analyticsCustomStart : undefined}
                  customEnd={analyticsTimeRange === 'custom' ? analyticsCustomEnd : undefined}
                  onCustomRangeChange={(start, end) => {
                    setAnalyticsCustomStart(start);
                    setAnalyticsCustomEnd(end);
                  }}
                />
              ) : isLoading ? (
                <>
                  <SkeletonRow />
                  <SkeletonRow />
                  <SkeletonRow />
                </>
              ) : filteredRequests.length === 0 ? (
                <div className="text-center py-12">
                  <p className="text-muted-foreground">No requests found</p>
                  <button
                    onClick={() => setIsNewPAModalOpen(true)}
                    className="mt-4 px-4 py-2 text-sm font-medium text-teal hover:bg-teal/10 rounded-lg transition-colors click-effect"
                  >
                    Create New PA Request
                  </button>
                </div>
              ) : (
                filteredRequests.map((request, index) => (
                  <RequestRow key={request.id} request={request} index={index} now={now} />
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right Column - hide when analytics */}
        {activeTab !== 'analytics' && (
        <div className="space-y-6">
          {/* Recent Activity */}
          <div className="bg-card rounded-2xl border shadow-soft">
            <div className="p-5 border-b flex items-center justify-between">
              <h2 className="font-bold text-foreground">Recent Activity</h2>
              <button className="text-xs text-teal font-medium hover:underline click-effect-link">See all</button>
            </div>
            <div className="p-4 space-y-2">
              {isLoading ? (
                <>
                  <div className="flex items-center gap-3 p-3 rounded-xl border border-gray-100">
                    <Skeleton className="w-2 h-2 rounded-full" />
                    <div className="flex-1 space-y-1">
                      <Skeleton className="h-3 w-24" />
                      <Skeleton className="h-2 w-32" />
                    </div>
                    <Skeleton className="h-3 w-12" />
                  </div>
                  <div className="flex items-center gap-3 p-3 rounded-xl border border-gray-100">
                    <Skeleton className="w-2 h-2 rounded-full" />
                    <div className="flex-1 space-y-1">
                      <Skeleton className="h-3 w-20" />
                      <Skeleton className="h-2 w-28" />
                    </div>
                    <Skeleton className="h-3 w-10" />
                  </div>
                </>
              ) : activity.length === 0 ? (
                <p className="text-center text-muted-foreground py-4">No recent activity</p>
              ) : (
                activity.map((item) => (
                  <ActivityItemComponent key={item.id} item={item} />
                ))
              )}
            </div>
          </div>

          {/* Weekly Summary */}
          <div className="bg-card rounded-2xl border shadow-soft p-5">
            <h3 className="font-bold text-foreground mb-4">This Week</h3>
            <div className="space-y-3">
              <div className="flex items-center justify-between py-2 border-b border-dashed">
                <span className="text-sm text-muted-foreground">Total Processed</span>
                <span className="font-bold text-foreground">{stats.total}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-b border-dashed">
                <span className="text-sm text-muted-foreground">Avg. Processing Time</span>
                <span className="font-bold text-foreground">4.2 min</span>
              </div>
              <div className="flex items-center justify-between py-2 border-b border-dashed">
                <span className="text-sm text-muted-foreground">AI Confidence Avg</span>
                <span className="font-bold text-teal">87%</span>
              </div>
              <div className="flex items-center justify-between py-2">
                <span className="text-sm text-muted-foreground">Success Rate</span>
                <span className="font-bold text-success">94%</span>
              </div>
            </div>
          </div>
        </div>
        )}
      </div>

      {/* Footer Status */}
      <div className="flex items-center justify-between py-4 border-t text-sm text-muted-foreground">
        <span>Monitoring athenahealth for signed encounters</span>
        <span>Last check: Just now • Interval: 30s</span>
      </div>

      {/* New PA Modal */}
      <NewPAModal 
        isOpen={isNewPAModalOpen} 
        onClose={() => setIsNewPAModalOpen(false)} 
      />
    </div>
  );
}
