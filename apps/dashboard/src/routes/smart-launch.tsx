import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { useEffect, useState } from 'react';
import { initiateSmartLaunch, type SmartLaunchContext } from '@/lib/smartAuth';
import { setEhrReturnUrl } from '@/lib/ehrExit';
import { 
  CheckCircle2, 
  AlertCircle, 
  Loader2, 
  User, 
  ArrowRight,
  Sparkles,
  Activity,
  Stethoscope
} from 'lucide-react';

export const Route = createFileRoute('/smart-launch')({
  component: SmartLaunchPage,
  validateSearch: (search: Record<string, unknown>) => ({
    iss: (search.iss as string) || '',
    launch: (search.launch as string) || '',
  }),
});

function SmartLaunchPage() {
  const { iss, launch } = Route.useSearch();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'loading' | 'ready' | 'analyzing' | 'error'>('loading');
  const [launchContext, setLaunchContext] = useState<SmartLaunchContext | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!iss || !launch) {
      setStatus('error');
      setError('Missing SMART launch parameters');
      return;
    }

    async function performLaunch() {
      try {
        const context = await initiateSmartLaunch(iss, launch);
        setLaunchContext(context);
        setStatus('ready');
        // Store EHR base URL so "Exit to EHR" can return user to athenahealth
        setEhrReturnUrl(iss);
      } catch (err) {
        setStatus('error');
        setError(err instanceof Error ? err.message : 'Launch failed');
      }
    }

    performLaunch();
  }, [iss, launch]);

  const handleAnalyze = () => {
    setStatus('analyzing');
    setTimeout(() => {
      navigate({ to: '/analysis/$transactionId', params: { transactionId: `pa-${Date.now()}` } });
    }, 2000);
  };

  // Loading State
  if (status === 'loading') {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center p-8">
        <div className="w-20 h-20 rounded-2xl bg-teal flex items-center justify-center mb-6 shadow-teal">
          <Loader2 className="w-10 h-10 text-white animate-spin" />
        </div>
        <h2 className="text-2xl font-bold text-foreground mb-2">Connecting to athenahealth</h2>
        <p className="text-muted-foreground">Establishing SMART on FHIR connection...</p>
      </div>
    );
  }

  // Analyzing State
  if (status === 'analyzing') {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center p-8">
        <div className="w-20 h-20 rounded-2xl bg-teal flex items-center justify-center mb-6 shadow-teal">
          <Sparkles className="w-10 h-10 text-white animate-pulse" />
        </div>
        <h2 className="text-2xl font-bold text-foreground mb-2">Analyzing Encounter</h2>
        <p className="text-muted-foreground mb-8">AI is processing clinical data and mapping to payer requirements...</p>
        <div className="w-64 h-1.5 bg-secondary rounded-full overflow-hidden">
          <div className="h-full bg-teal rounded-full animate-[loading_1.5s_ease-in-out_infinite]" style={{ width: '60%' }} />
        </div>
      </div>
    );
  }

  // Error State
  if (status === 'error') {
    return (
      <div className="min-h-[70vh] flex flex-col items-center justify-center text-center p-8">
        <div className="w-20 h-20 rounded-2xl bg-destructive/10 flex items-center justify-center mb-6">
          <AlertCircle className="w-10 h-10 text-destructive" />
        </div>
        <h2 className="text-2xl font-bold text-foreground mb-2">Connection Error</h2>
        <p className="text-muted-foreground mb-8 max-w-md">{error}</p>
        <div className="flex gap-3">
          <button 
            onClick={() => navigate({ to: '/' })} 
            className="px-5 py-2.5 text-sm font-medium border rounded-xl hover:bg-secondary transition-colors"
          >
            Go to Dashboard
          </button>
          <button 
            onClick={() => window.location.reload()} 
            className="px-5 py-2.5 text-sm font-semibold bg-teal text-white rounded-xl hover:bg-teal/90 transition-colors shadow-teal"
          >
            Retry Connection
          </button>
        </div>
      </div>
    );
  }

  // Ready State
  const patient = { 
    name: 'Sarah Johnson', 
    mrn: launchContext?.patientId || '10045892', 
    dob: '03/15/1968' 
  };
  const encounter = { 
    id: launchContext?.encounterId || 'ENC-2026-0201', 
    date: 'February 1, 2026', 
    provider: 'Dr. Amanda Martinez',
    type: 'Office Visit'
  };

  return (
    <div className="p-6 max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="text-center space-y-3">
        <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-success/10 text-success text-sm font-medium">
          <CheckCircle2 className="w-4 h-4" />
          Connected to athenahealth
        </div>
        <h1 className="text-3xl font-bold text-foreground">Encounter Detected</h1>
        <p className="text-muted-foreground">
          A signed encounter is ready for prior authorization analysis
        </p>
      </div>

      {/* Patient & Encounter Cards */}
      <div className="grid grid-cols-2 gap-4">
        <div className="bg-card rounded-2xl border shadow-soft p-5">
          <div className="flex items-center gap-2 text-sm text-muted-foreground mb-3">
            <User className="w-4 h-4 text-teal" />
            <span className="font-medium">Patient</span>
          </div>
          <p className="text-xl font-bold text-foreground mb-3">{patient.name}</p>
          <div className="flex gap-6 text-sm">
            <div>
              <p className="text-muted-foreground text-xs mb-1">MRN</p>
              <p className="font-mono font-semibold text-foreground">{patient.mrn}</p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs mb-1">DOB</p>
              <p className="font-semibold text-foreground">{patient.dob}</p>
            </div>
          </div>
        </div>

        <div className="bg-card rounded-2xl border shadow-soft p-5">
          <div className="flex items-center gap-2 text-sm text-muted-foreground mb-3">
            <Stethoscope className="w-4 h-4 text-teal" />
            <span className="font-medium">Encounter</span>
          </div>
          <div className="flex items-center justify-between mb-3">
            <p className="text-xl font-bold text-foreground">{encounter.type}</p>
            <span className="px-2.5 py-1 rounded-lg bg-success/10 text-success text-xs font-semibold">
              Signed
            </span>
          </div>
          <div className="flex gap-6 text-sm">
            <div>
              <p className="text-muted-foreground text-xs mb-1">Date</p>
              <p className="font-semibold text-foreground">{encounter.date}</p>
            </div>
            <div>
              <p className="text-muted-foreground text-xs mb-1">Provider</p>
              <p className="font-semibold text-foreground">{encounter.provider}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Action Card */}
      <div className="bg-card rounded-2xl border shadow-soft p-6">
        <div className="flex items-start gap-5">
          <div className="w-14 h-14 rounded-2xl bg-teal flex items-center justify-center shadow-teal flex-shrink-0">
            <Activity className="w-7 h-7 text-white" />
          </div>
          <div className="flex-1">
            <h3 className="font-bold text-xl text-foreground">Ready for Analysis</h3>
            <p className="text-muted-foreground mt-2 mb-5 leading-relaxed">
              AuthScript will extract clinical data from the encounter notes, match it against 
              payer requirements, and generate a prior authorization request for your review.
            </p>
            <button 
              onClick={handleAnalyze}
              className="px-5 py-2.5 bg-teal text-white rounded-xl font-semibold hover:bg-teal/90 transition-all shadow-teal flex items-center gap-2"
            >
              <Sparkles className="w-4 h-4" />
              Run PA Analysis
              <ArrowRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
