import { useState, useEffect } from 'react';
import { createFileRoute, Link } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import { 
  ArrowLeft, 
  Check, 
  X, 
  AlertCircle, 
  AlertTriangle,
  CheckCircle2, 
  Sparkles,
  FileText,
  User,
  Building2,
  Shield,
  Download,
  Edit2,
  Send,
  ChevronRight,
  Stethoscope,
  Save,
  Printer,
} from 'lucide-react';
import { getPARequest, updatePARequest, submitPARequest, type PARequest } from '@/lib/store';
import { openPAPdf } from '@/lib/pdfGenerator';
import { LoadingSpinner, Skeleton } from '@/components/LoadingSpinner';

export const Route = createFileRoute('/analysis/$transactionId')({
  component: AnalysisPage,
});

// Display confidence: never show 0%
const displayConfidence = (c: number) => Math.max(1, c);

// Low confidence threshold: same as dashboard "Needs Attention"
const LOW_CONFIDENCE_THRESHOLD = 70;

// Circular Progress Ring
function ProgressRing({ value, size = 140 }: { value: number; size?: number }) {
  const display = displayConfidence(value);
  const strokeWidth = 10;
  const radius = (size - strokeWidth) / 2;
  const circumference = radius * 2 * Math.PI;
  const offset = circumference - (display / 100) * circumference;
  
  const colorClass = display >= 80 ? 'text-success stroke-success/20' : 
                     display >= 60 ? 'text-warning stroke-warning/20' : 
                     'text-destructive stroke-destructive/20';
  
  return (
    <div className="relative flex items-center justify-center" style={{ width: size, height: size }}>
      <svg className="transform -rotate-90" width={size} height={size}>
        <circle
          className={colorClass.split(' ')[1]}
          strokeWidth={strokeWidth}
          fill="transparent"
          r={radius}
          cx={size / 2}
          cy={size / 2}
        />
        <circle
          className={cn('transition-all duration-1000 ease-out', colorClass.split(' ')[0])}
          strokeWidth={strokeWidth}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          fill="transparent"
          stroke="currentColor"
          r={radius}
          cx={size / 2}
          cy={size / 2}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className={cn('text-4xl font-bold', colorClass.split(' ')[0])}>{display}%</span>
        <span className="text-xs text-muted-foreground mt-1">confidence</span>
      </div>
    </div>
  );
}

// Editable Field
function EditableField({ 
  label, 
  value, 
  onChange, 
  isEditing,
  mono 
}: { 
  label: string; 
  value: string; 
  onChange: (value: string) => void;
  isEditing: boolean;
  mono?: boolean;
}) {
  return (
    <div className="flex items-center justify-between py-2.5 border-b border-dashed last:border-0">
      <span className="text-sm text-muted-foreground">{label}</span>
      {isEditing ? (
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={cn(
            'text-sm font-medium text-foreground bg-secondary/50 px-2 py-1 rounded border focus:outline-none focus:ring-2 focus:ring-teal',
            mono && 'font-mono'
          )}
        />
      ) : (
        <span className={cn('text-sm font-medium text-foreground', mono && 'font-mono')}>{value}</span>
      )}
    </div>
  );
}

// Criteria Item
function CriteriaItem({ 
  met, 
  label, 
  onToggle, 
  isEditing 
}: { 
  met: boolean | null; 
  label: string; 
  onToggle?: () => void;
  isEditing: boolean;
}) {
  const styles = {
    true: { bg: 'bg-success/5', border: 'border-success/30', icon: 'bg-success', text: 'text-success' },
    false: { bg: 'bg-destructive/5', border: 'border-destructive/30', icon: 'bg-destructive', text: 'text-destructive' },
    null: { bg: 'bg-warning/5', border: 'border-warning/30', icon: 'bg-warning', text: 'text-warning' },
  };
  const style = styles[String(met) as keyof typeof styles];
  
  return (
    <div 
      className={cn(
        'flex items-center gap-3 p-3 rounded-xl border transition-all',
        style.bg, 
        style.border,
        isEditing && 'cursor-pointer hover:opacity-80'
      )}
      onClick={isEditing ? onToggle : undefined}
    >
      <div className={cn('w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0 text-white', style.icon)}>
        {met === true && <Check className="w-3.5 h-3.5" />}
        {met === false && <X className="w-3.5 h-3.5" />}
        {met === null && <AlertCircle className="w-3.5 h-3.5" />}
      </div>
      <span className="text-sm text-foreground flex-1">{label}</span>
      {isEditing && (
        <span className="text-xs text-muted-foreground">Click to toggle</span>
      )}
    </div>
  );
}

function AnalysisPage() {
  const { transactionId } = Route.useParams();
  const [request, setRequest] = useState<PARequest | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [editedData, setEditedData] = useState<Partial<PARequest>>({});

  // Load request data
  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true);
      // Simulate loading delay for demo
      await new Promise(r => setTimeout(r, 600));
      
      const data = getPARequest(transactionId);
      if (data) {
        setRequest(data);
        setEditedData({
          diagnosis: data.diagnosis,
          diagnosisCode: data.diagnosisCode,
          serviceDate: data.serviceDate,
          placeOfService: data.placeOfService,
          clinicalSummary: data.clinicalSummary,
          criteria: data.criteria,
        });
      }
      setIsLoading(false);
    };
    
    loadData();
  }, [transactionId]);

  // Loading state
  if (isLoading) {
    return (
      <div className="p-6 space-y-6">
        {/* Header skeleton */}
        <div className="flex items-center gap-4">
          <Skeleton className="w-10 h-10 rounded-xl" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-64" />
            <Skeleton className="h-4 w-48" />
          </div>
        </div>
        
        <div className="grid grid-cols-3 gap-6">
          {/* Left column skeleton */}
          <div className="col-span-2 space-y-6">
            <div className="bg-white rounded-2xl border border-gray-200 p-6 space-y-4">
              <Skeleton className="h-5 w-40" />
              <div className="grid grid-cols-2 gap-4">
                <Skeleton className="h-20 rounded-xl" />
                <Skeleton className="h-20 rounded-xl" />
                <Skeleton className="h-20 rounded-xl" />
                <Skeleton className="h-20 rounded-xl" />
              </div>
            </div>
            <div className="bg-white rounded-2xl border border-gray-200 p-6 space-y-4">
              <Skeleton className="h-5 w-48" />
              <Skeleton className="h-24 rounded-xl" />
            </div>
          </div>
          
          {/* Right column skeleton */}
          <div className="space-y-6">
            <div className="bg-white rounded-2xl border border-gray-200 p-6">
              <Skeleton className="h-5 w-32 mx-auto mb-4" />
              <Skeleton className="h-32 w-32 rounded-full mx-auto" />
            </div>
            <div className="bg-white rounded-2xl border border-gray-200 p-6 space-y-3">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-12 rounded-xl" />
              <Skeleton className="h-12 rounded-xl" />
              <Skeleton className="h-12 rounded-xl" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!request) {
    return (
      <div className="p-6 flex flex-col items-center justify-center min-h-[50vh]">
        <AlertCircle className="w-12 h-12 text-muted-foreground mb-4" />
        <h2 className="text-lg font-semibold text-foreground mb-2">Request Not Found</h2>
        <p className="text-muted-foreground mb-4">The PA request you're looking for doesn't exist.</p>
        <Link to="/" className="text-teal hover:underline">Return to Dashboard</Link>
      </div>
    );
  }

  const isSubmitted = request.status === 'submitted' || request.status === 'approved';

  const handleSaveEdits = () => {
    const updated = updatePARequest(transactionId, editedData);
    if (updated) {
      setRequest(updated);
      setIsEditing(false);
    }
  };

  const handleCancelEdits = () => {
    setEditedData({
      diagnosis: request.diagnosis,
      diagnosisCode: request.diagnosisCode,
      serviceDate: request.serviceDate,
      placeOfService: request.placeOfService,
      clinicalSummary: request.clinicalSummary,
      criteria: request.criteria,
    });
    setIsEditing(false);
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);
    await new Promise(r => setTimeout(r, 1500));
    const updated = submitPARequest(transactionId);
    if (updated) {
      setRequest(updated);
    }
    setIsSubmitting(false);
  };

  const handleDownloadPdf = () => {
    openPAPdf(request);
  };

  const handleToggleCriteria = (index: number) => {
    if (!editedData.criteria) return;
    const newCriteria = [...editedData.criteria];
    const current = newCriteria[index].met;
    newCriteria[index] = {
      ...newCriteria[index],
      met: current === true ? false : current === false ? null : true
    };
    setEditedData({ ...editedData, criteria: newCriteria });
  };

  const displayData = isEditing ? editedData : request;
  const isLowConfidence = request.status === 'ready' && request.confidence < LOW_CONFIDENCE_THRESHOLD;

  return (
    <div className="p-6 space-y-6 animate-fade-in">
      {/* Low confidence banner */}
      {isLowConfidence && !isSubmitted && (
        <div className="flex items-center gap-3 px-4 py-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-900">
          <div className="flex-shrink-0 w-10 h-10 rounded-lg bg-amber-100 flex items-center justify-center">
            <AlertTriangle className="w-5 h-5 text-amber-600" />
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-semibold text-amber-900">Low confidence — manual review required</p>
            <p className="text-sm text-amber-800 mt-0.5">
              This request has AI confidence below {LOW_CONFIDENCE_THRESHOLD}%. Please review clinical summary and policy criteria before submitting.
            </p>
          </div>
          <span className="flex-shrink-0 px-3 py-1 rounded-lg bg-amber-200 text-amber-900 text-sm font-bold">
            {displayConfidence(request.confidence)}%
          </span>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link 
            to="/" 
            className="p-2.5 rounded-xl bg-secondary hover:bg-secondary/80 transition-colors click-effect"
          >
            <ArrowLeft className="w-5 h-5 text-foreground" />
          </Link>
          <div>
            <div className="flex items-center gap-3 flex-wrap">
              <h1 className="text-xl font-bold text-foreground">Prior Authorization Review</h1>
              <span className="px-2 py-0.5 rounded text-xs font-mono text-muted-foreground bg-secondary">
                {request.id}
              </span>
              {isLowConfidence && !isSubmitted && (
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-100 text-amber-800 text-xs font-bold border border-amber-300">
                  <AlertTriangle className="w-3.5 h-3.5" />
                  Low confidence
                </span>
              )}
              {isSubmitted && (
                <span className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-success/10 text-success text-xs font-bold">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  Submitted
                </span>
              )}
              {isEditing && (
                <span className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-warning/10 text-warning text-xs font-bold">
                  <Edit2 className="w-3.5 h-3.5" />
                  Editing
                </span>
              )}
            </div>
            <p className="text-sm text-muted-foreground mt-0.5">
              {request.patient.name} • {request.procedureCode} • {request.payer}
            </p>
          </div>
        </div>

        {!isSubmitted && (
          <div className="flex items-center gap-3">
            {isEditing ? (
              <>
                <button 
                  onClick={handleCancelEdits}
                  className="px-4 py-2.5 text-sm border rounded-xl hover:bg-secondary transition-colors font-medium click-effect"
                >
                  Cancel
                </button>
                <button 
                  onClick={handleSaveEdits}
                  className="px-4 py-2.5 text-sm bg-teal text-white rounded-xl hover:bg-teal/90 transition-colors font-semibold flex items-center gap-2 click-effect-primary"
                >
                  <Save className="w-4 h-4" />
                  Save Changes
                </button>
              </>
            ) : (
              <>
                <button 
                  onClick={() => setIsEditing(true)}
                  className="px-4 py-2.5 text-sm border rounded-xl hover:bg-secondary transition-colors flex items-center gap-2 font-medium click-effect"
                >
                  <Edit2 className="w-4 h-4" />
                  Edit
                </button>
                <button 
                  onClick={handleDownloadPdf}
                  className="px-4 py-2.5 text-sm border rounded-xl hover:bg-secondary transition-colors flex items-center gap-2 font-medium click-effect"
                >
                  <Printer className="w-4 h-4" />
                  Print/PDF
                </button>
                <button
                  onClick={handleSubmit}
                  disabled={isSubmitting}
                  className="px-5 py-2.5 text-sm font-semibold bg-teal text-white rounded-xl hover:bg-teal/90 disabled:opacity-70 transition-all shadow-teal flex items-center gap-2 min-w-[160px] justify-center click-effect-primary"
                >
                  {isSubmitting ? (
                    <>
                      <LoadingSpinner size="sm" className="border-white border-t-transparent" />
                      <span>Submitting...</span>
                    </>
                  ) : (
                    <>
                      <Send className="w-4 h-4" />
                      Confirm & Submit
                    </>
                  )}
                </button>
              </>
            )}
          </div>
        )}

        {isSubmitted && (
          <button 
            onClick={handleDownloadPdf}
            className="px-4 py-2.5 text-sm border rounded-xl hover:bg-secondary transition-colors flex items-center gap-2 font-medium click-effect"
          >
            <Download className="w-4 h-4" />
            Download PDF
          </button>
        )}
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Left Column - Main Content */}
        <div className="col-span-2 space-y-6">
          {/* Patient Information */}
          <div className="bg-card rounded-2xl border shadow-soft p-6">
            <h2 className="font-bold text-foreground mb-4 flex items-center gap-2">
              <User className="w-5 h-5 text-teal" />
              Patient Information
            </h2>
            <div className="grid grid-cols-2 gap-4">
              <div className="p-4 rounded-xl bg-secondary/50 border">
                <p className="text-xs text-muted-foreground mb-1">Patient Name</p>
                <p className="font-semibold text-foreground">{request.patient.name}</p>
              </div>
              <div className="p-4 rounded-xl bg-secondary/50 border">
                <p className="text-xs text-muted-foreground mb-1">MRN</p>
                <p className="font-semibold text-foreground font-mono">{request.patient.mrn}</p>
              </div>
              <div className="p-4 rounded-xl bg-secondary/50 border">
                <p className="text-xs text-muted-foreground mb-1">Date of Birth</p>
                <p className="font-semibold text-foreground">{request.patient.dob}</p>
              </div>
              <div className="p-4 rounded-xl bg-secondary/50 border">
                <p className="text-xs text-muted-foreground mb-1">Member ID</p>
                <p className="font-semibold text-foreground font-mono">{request.patient.memberId}</p>
              </div>
            </div>
          </div>

          {/* Procedure & Payer */}
          <div className="bg-card rounded-2xl border shadow-soft p-6">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs text-muted-foreground mb-1 font-medium uppercase tracking-wide">Requested Procedure</p>
                <p className="text-xl font-bold text-foreground">{request.procedureCode} — {request.procedureName}</p>
                <div className="flex items-center gap-2 mt-3 text-sm text-muted-foreground">
                  <Building2 className="w-4 h-4 text-teal" />
                  <span>{request.payer}</span>
                </div>
              </div>
              <span className="px-3 py-1.5 rounded-lg bg-teal/10 text-teal text-xs font-bold">
                PA Required
              </span>
            </div>
          </div>

          {/* PA Form Data */}
          <div className="bg-card rounded-2xl border shadow-soft p-6">
            <div className="flex items-center justify-between mb-5">
              <h2 className="font-bold text-foreground flex items-center gap-2">
                <FileText className="w-5 h-5 text-teal" />
                PA Form Data
              </h2>
              <span className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-teal/10 text-teal text-xs font-semibold">
                <Sparkles className="w-3 h-3" />
                {isEditing ? 'Editing' : 'AI Auto-filled'}
              </span>
            </div>
            
            <div className="grid grid-cols-2 gap-x-8">
              <EditableField 
                label="Patient Name" 
                value={request.patient.name} 
                onChange={() => {}}
                isEditing={false}
              />
              <EditableField 
                label="Date of Birth" 
                value={request.patient.dob} 
                onChange={() => {}}
                isEditing={false}
              />
              <EditableField 
                label="Member ID" 
                value={request.patient.memberId} 
                onChange={() => {}}
                isEditing={false}
                mono
              />
              <EditableField 
                label="Diagnosis Code" 
                value={`${displayData.diagnosisCode} - ${displayData.diagnosis}`} 
                onChange={(v) => {
                  const parts = v.split(' - ');
                  setEditedData({ 
                    ...editedData, 
                    diagnosisCode: parts[0] || '',
                    diagnosis: parts[1] || ''
                  });
                }}
                isEditing={isEditing}
              />
              <EditableField 
                label="Procedure Code" 
                value={request.procedureCode} 
                onChange={() => {}}
                isEditing={false}
              />
              <EditableField 
                label="Service Date" 
                value={displayData.serviceDate || ''} 
                onChange={(v) => setEditedData({ ...editedData, serviceDate: v })}
                isEditing={isEditing}
              />
              <EditableField 
                label="Ordering Provider" 
                value={request.provider} 
                onChange={() => {}}
                isEditing={false}
              />
              <EditableField 
                label="Place of Service" 
                value={displayData.placeOfService || ''} 
                onChange={(v) => setEditedData({ ...editedData, placeOfService: v })}
                isEditing={isEditing}
              />
            </div>

            <div className="mt-6 p-4 bg-secondary/50 rounded-xl border">
              <div className="flex items-center gap-2 mb-2">
                <Stethoscope className="w-4 h-4 text-teal" />
                <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Clinical Summary</p>
              </div>
              {isEditing ? (
                <textarea
                  value={editedData.clinicalSummary || ''}
                  onChange={(e) => setEditedData({ ...editedData, clinicalSummary: e.target.value })}
                  className="w-full text-sm leading-relaxed text-foreground bg-card p-3 rounded-lg border focus:outline-none focus:ring-2 focus:ring-teal min-h-[120px]"
                />
              ) : (
                <p className="text-sm leading-relaxed text-foreground">
                  {request.clinicalSummary}
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Right Column - Sidebar */}
        <div className="space-y-6">
          {/* Confidence Score */}
          <div className={cn(
            'rounded-2xl border shadow-soft p-6',
            isLowConfidence ? 'bg-amber-50/50 border-amber-200' : 'bg-card'
          )}>
            <h2 className="font-bold text-foreground mb-4 flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-teal" />
              AI Confidence
              {isLowConfidence && (
                <span className="ml-auto inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 text-[10px] font-bold">
                  <AlertTriangle className="w-3 h-3" />
                  Low
                </span>
              )}
            </h2>
            <div className="flex justify-center py-4">
              <ProgressRing value={request.confidence} />
            </div>
            <p className={cn(
              'text-center text-sm font-medium mt-4 p-3 rounded-lg',
              displayConfidence(request.confidence) >= 80 ? 'bg-success/10 text-success' : 
              displayConfidence(request.confidence) >= 60 ? 'bg-warning/10 text-warning' : 
              'bg-destructive/10 text-destructive'
            )}>
              {displayConfidence(request.confidence) >= 80 ? 'High confidence — ready for submission' : 
               displayConfidence(request.confidence) >= 60 ? 'Medium confidence — review recommended' : 
               'Low confidence — manual review required'}
            </p>
          </div>

          {/* Policy Criteria */}
          <div className="bg-card rounded-2xl border shadow-soft p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-bold text-foreground flex items-center gap-2">
                <Shield className="w-5 h-5 text-teal" />
                Policy Criteria
              </h2>
              <span className="px-2 py-1 rounded-md bg-success/10 text-success text-xs font-bold">
                {(displayData.criteria || []).filter((c: { met: boolean | null; label: string }) => c.met === true).length}/{(displayData.criteria || []).length} met
              </span>
            </div>
            <p className="text-xs text-muted-foreground mb-4">{request.payer} — {request.procedureName}</p>
            
            <div className="space-y-2">
              {(displayData.criteria || []).map((c: { met: boolean | null; label: string }, i: number) => (
                <CriteriaItem 
                  key={i} 
                  met={c.met} 
                  label={c.label} 
                  isEditing={isEditing}
                  onToggle={() => handleToggleCriteria(i)}
                />
              ))}
            </div>
            {isEditing && (
              <p className="text-xs text-muted-foreground mt-3 text-center">
                Click criteria to toggle status
              </p>
            )}
          </div>

          {/* Actions */}
          {!isSubmitted && !isEditing && (
            <div className="space-y-3">
              <button
                onClick={handleSubmit}
                disabled={isSubmitting}
                className="w-full px-5 py-3.5 text-sm font-semibold bg-teal text-white rounded-xl hover:bg-teal/90 disabled:opacity-70 transition-all shadow-teal flex items-center justify-center gap-2 click-effect-primary"
              >
                {isSubmitting ? (
                  <>
                    <LoadingSpinner size="sm" className="border-white border-t-transparent" />
                    <span>Submitting to athenahealth...</span>
                  </>
                ) : (
                  <>
                    <Send className="w-4 h-4" />
                    Confirm & Submit to athenahealth
                  </>
                )}
              </button>
              <button 
                onClick={handleDownloadPdf}
                className="w-full px-5 py-3 text-sm font-medium border rounded-xl hover:bg-secondary transition-colors text-foreground flex items-center justify-center gap-2 click-effect"
              >
                <Printer className="w-4 h-4" />
                Preview & Print PDF
              </button>
            </div>
          )}

          {isSubmitted && (
            <div className="bg-success/5 border border-success/30 rounded-2xl p-6 text-center">
              <div className="w-14 h-14 rounded-2xl bg-success flex items-center justify-center mx-auto mb-4">
                <CheckCircle2 className="w-7 h-7 text-white" />
              </div>
              <p className="font-bold text-success text-lg">Successfully Submitted</p>
              <p className="text-sm text-muted-foreground mt-2">
                PA request has been submitted to athenahealth and attached to the patient chart.
              </p>
              <div className="flex flex-col gap-2 mt-4">
                <button
                  onClick={handleDownloadPdf}
                  className="px-4 py-2 text-sm font-medium text-teal hover:bg-teal/10 rounded-lg transition-colors click-effect"
                >
                  Download PDF Copy
                </button>
                <Link
                  to="/"
                  className="inline-flex items-center justify-center gap-2 text-sm font-semibold text-foreground hover:text-teal click-effect-link"
                >
                  Back to Dashboard
                  <ChevronRight className="w-4 h-4" />
                </Link>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
