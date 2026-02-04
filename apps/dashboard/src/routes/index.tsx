import { useState, useEffect } from 'react';
import { createFileRoute, Link } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import {
  CheckCircle2,
  Clock,
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
} from 'lucide-react';
import { getPARequests, getPAStats, type PARequest } from '@/lib/store';
import { generateMockActivity, type ActivityItem } from '@/lib/mockData';
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
          {request.status === 'ready' ? 'Ready' : request.status === 'processing' ? 'Processing' : request.status}
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
    processing: 'bg-warning/10 text-warning border-warning/20',
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
  const [requests, setRequests] = useState<PARequest[]>([]);
  const [stats, setStats] = useState({ ready: 0, processing: 0, submitted: 0, attention: 0, total: 0 });
  const [activity, setActivity] = useState<ActivityItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [now, setNow] = useState(() => Date.now());

  // Update "now" every minute for relative time display (avoids impure Date.now() during render)
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 60000);
    return () => clearInterval(id);
  }, []);

  // Load data with initial loading state
  useEffect(() => {
    const loadData = async (initial = false) => {
      if (initial) {
        // Simulate initial load delay for demo
        await new Promise(r => setTimeout(r, 800));
      }
      
      const paRequests = getPARequests();
      setRequests(paRequests);
      setStats(getPAStats());
      setActivity(generateMockActivity(paRequests));
      
      if (initial) {
        setIsLoading(false);
      }
    };

    loadData(true);
    
    // Refresh every 5 seconds
    const interval = setInterval(() => loadData(false), 5000);
    return () => clearInterval(interval);
  }, []);

  const currentDate = new Date().toLocaleDateString('en-US', { 
    weekday: 'long', 
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  });

  const tabs = [
    { id: 'pending', label: 'Pending Review', icon: ClipboardCheck, count: stats.ready },
    { id: 'processing', label: 'Processing', icon: Zap, count: stats.processing },
    { id: 'history', label: 'History', icon: FileText },
    { id: 'analytics', label: 'Analytics', icon: BarChart3 },
  ];

  const filteredRequests = requests.filter(r => {
    if (activeTab === 'pending') return r.status === 'ready';
    if (activeTab === 'processing') return r.status === 'processing';
    if (activeTab === 'history') return r.status === 'submitted' || r.status === 'approved' || r.status === 'denied';
    return true;
  });

  return (
    <div className="p-6 space-y-6 animate-fade-in">
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
        <div className="grid grid-cols-4 gap-4">
          <StatCard 
            label="Ready for Review" 
            value={stats.ready} 
            change={stats.ready > 0 ? `${stats.ready} pending` : undefined}
            positive 
            icon={CheckCircle2}
          />
          <StatCard 
            label="Processing" 
            value={stats.processing}
            icon={Clock}
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
            <span className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-teal/10 text-teal font-medium">
              <Activity className="w-4 h-4" />
              {stats.processing} in progress
            </span>
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
      <div className="grid grid-cols-3 gap-6">
        {/* Pending Requests */}
        <div className="col-span-2 bg-card rounded-2xl border shadow-soft">
          <div className="p-5 border-b flex items-center justify-between">
            <div>
              <h2 className="text-lg font-bold text-foreground">
                {activeTab === 'pending' && 'Pending Requests'}
                {activeTab === 'processing' && 'Processing'}
                {activeTab === 'history' && 'History'}
                {activeTab === 'analytics' && 'Analytics'}
              </h2>
              <p className="text-sm text-muted-foreground">{filteredRequests.length} requests</p>
            </div>
            <button
              onClick={() => setIsNewPAModalOpen(true)}
              className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-teal hover:bg-teal/10 rounded-lg transition-colors click-effect"
            >
              <Plus className="w-4 h-4" />
              New Request
            </button>
          </div>
          <div className="p-4 space-y-3">
            {isLoading ? (
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

        {/* Right Column */}
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
