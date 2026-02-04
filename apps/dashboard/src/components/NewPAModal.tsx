import { useState, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { useNavigate } from '@tanstack/react-router';
import { cn } from '@/lib/utils';
import {
  X,
  Search,
  User,
  FileText,
  Pill,
  ChevronRight,
  Check,
  Sparkles,
  Brain,
  Shield,
  FileCheck,
} from 'lucide-react';
import {
  PATIENTS,
  PROCEDURES,
  MEDICATIONS,
  DIAGNOSES,
  Patient,
  Procedure,
  Medication,
} from '@/lib/mockData';
import { createPARequest, processPARequest } from '@/lib/store';
import { LoadingSpinner } from './LoadingSpinner';

interface NewPAModalProps {
  isOpen: boolean;
  onClose: () => void;
}

type Step = 'patient' | 'service' | 'diagnosis' | 'processing';

const PROCESSING_STEPS = [
  { icon: FileText, label: 'Reading clinical notes...' },
  { icon: Brain, label: 'AI analyzing medical necessity...' },
  { icon: Shield, label: 'Mapping to payer requirements...' },
  { icon: FileCheck, label: 'Generating PA form...' },
];

export function NewPAModal({ isOpen, onClose }: NewPAModalProps) {
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>('patient');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [serviceType, setServiceType] = useState<'procedure' | 'medication'>('procedure');
  const [selectedService, setSelectedService] = useState<Procedure | Medication | null>(null);
  const [_selectedDiagnosis, setSelectedDiagnosis] = useState<{ code: string; name: string } | null>(null);
  const [processingStep, setProcessingStep] = useState(0);
  const [isTransitioning, setIsTransitioning] = useState(false);

  // Animate processing steps
  useEffect(() => {
    if (step === 'processing') {
      const interval = setInterval(() => {
        setProcessingStep(prev => {
          if (prev < PROCESSING_STEPS.length - 1) {
            return prev + 1;
          }
          return prev;
        });
      }, 800);
      return () => clearInterval(interval);
    } else {
      setProcessingStep(0);
    }
  }, [step]);

  if (!isOpen) return null;

  const resetAndClose = () => {
    setStep('patient');
    setSearchQuery('');
    setSelectedPatient(null);
    setSelectedService(null);
    setSelectedDiagnosis(null);
    setIsTransitioning(false);
    onClose();
  };

  // Transition to next step with loading effect
  const transitionToStep = (nextStep: Step, delay: number = 400) => {
    setIsTransitioning(true);
    setTimeout(() => {
      setStep(nextStep);
      setSearchQuery('');
      setIsTransitioning(false);
    }, delay);
  };

  const handlePatientSelect = (patient: Patient) => {
    setSelectedPatient(patient);
    transitionToStep('service');
  };

  const handleServiceSelect = (service: Procedure | Medication) => {
    setSelectedService(service);
    transitionToStep('diagnosis');
  };

  const handleDiagnosisSelect = async (diagnosis: { code: string; name: string }) => {
    setSelectedDiagnosis(diagnosis);
    transitionToStep('processing', 500);
    
    // Create the PA request
    const newRequest = createPARequest(
      selectedPatient!,
      selectedService!,
      diagnosis
    );
    
    // Process with AI (simulated)
    await processPARequest(newRequest.id);
    
    // Navigate to the review page
    resetAndClose();
    navigate({ to: '/analysis/$transactionId', params: { transactionId: newRequest.id } });
  };

  const filteredPatients = PATIENTS.filter(p =>
    p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    p.mrn.includes(searchQuery)
  );

  const filteredServices = serviceType === 'procedure'
    ? PROCEDURES.filter(p => 
        p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        p.code.includes(searchQuery)
      )
    : MEDICATIONS.filter(m =>
        m.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        m.code.includes(searchQuery)
      );

  const filteredDiagnoses = DIAGNOSES.filter(d =>
    d.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    d.code.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Use portal to render modal outside of constrained containers
  return createPortal(
    <div className="fixed inset-0 z-[9999] flex items-center justify-center overflow-y-auto">
      {/* Backdrop - covers entire viewport */}
      <div 
        className="fixed inset-0 bg-black/60 backdrop-blur-sm" 
        onClick={resetAndClose}
        aria-hidden="true"
        style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0 }}
      />
      
      {/* Modal - centered */}
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[85vh] overflow-hidden border border-gray-200 mx-4 my-8 z-[10000]">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-gray-200 bg-white">
          <div>
            <h2 className="text-lg font-bold text-gray-900">New Prior Authorization</h2>
            <p className="text-sm text-gray-500">
              {step === 'patient' && 'Select a patient'}
              {step === 'service' && 'Select procedure or medication'}
              {step === 'diagnosis' && 'Select diagnosis'}
              {step === 'processing' && 'Processing...'}
            </p>
          </div>
          <button 
            onClick={resetAndClose}
            className="p-2 rounded-lg hover:bg-gray-100 transition-colors click-effect"
          >
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        {/* Progress Steps */}
        <div className="flex items-center gap-2 px-5 py-3 bg-gray-50 border-b border-gray-200">
          {['patient', 'service', 'diagnosis'].map((s, i) => (
            <div key={s} className="flex items-center gap-2">
              <div className={cn(
                'w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold',
                step === s && 'bg-teal text-white',
                (step === 'service' && s === 'patient') || 
                (step === 'diagnosis' && (s === 'patient' || s === 'service')) ||
                (step === 'processing')
                  ? 'bg-teal text-white'
                  : step !== s && 'bg-gray-200 text-gray-500'
              )}>
                {(step === 'service' && s === 'patient') || 
                 (step === 'diagnosis' && (s === 'patient' || s === 'service')) ||
                 (step === 'processing')
                  ? <Check className="w-3 h-3" />
                  : i + 1}
              </div>
              <span className={cn(
                'text-sm capitalize',
                step === s ? 'text-teal font-medium' : 'text-gray-500'
              )}>
                {s}
              </span>
              {i < 2 && <ChevronRight className="w-4 h-4 text-gray-400" />}
            </div>
          ))}
        </div>

        {/* Content */}
        <div className="p-5 overflow-y-auto max-h-[50vh] bg-white relative">
          {/* Step Transition Loading Overlay */}
          {isTransitioning && (
            <div className="absolute inset-0 bg-white/90 backdrop-blur-sm z-10 flex flex-col items-center justify-center">
              <div className="relative">
                <div className="w-12 h-12 rounded-xl bg-teal/10 flex items-center justify-center">
                  <LoadingSpinner size="md" />
                </div>
              </div>
              <p className="text-sm text-gray-500 mt-3 animate-pulse">Loading...</p>
            </div>
          )}

          {/* Processing State */}
          {step === 'processing' && !isTransitioning && (
            <div className="flex flex-col items-center justify-center py-8">
              {/* Animated Logo */}
              <div className="relative mb-6">
                <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-teal to-teal/80 flex items-center justify-center shadow-lg">
                  <Sparkles className="w-10 h-10 text-white animate-pulse" />
                </div>
                <div className="absolute -bottom-2 -right-2 w-8 h-8 bg-white rounded-full shadow-md flex items-center justify-center">
                  <LoadingSpinner size="sm" />
                </div>
              </div>

              <h3 className="text-xl font-bold text-gray-900 mb-2">Processing PA Request</h3>
              <p className="text-gray-500 text-center max-w-sm mb-6">
                Our AI is analyzing the clinical data
              </p>

              {/* Processing Steps */}
              <div className="w-full max-w-sm space-y-3">
                {PROCESSING_STEPS.map((item, index) => {
                  const Icon = item.icon;
                  const isComplete = index < processingStep;
                  const isCurrent = index === processingStep;
                  const isPending = index > processingStep;

                  return (
                    <div
                      key={item.label}
                      className={cn(
                        'flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-500',
                        isComplete && 'bg-teal/5',
                        isCurrent && 'bg-teal/10 border border-teal/20',
                        isPending && 'opacity-40'
                      )}
                    >
                      <div className={cn(
                        'w-8 h-8 rounded-lg flex items-center justify-center transition-all duration-500',
                        isComplete && 'bg-teal text-white',
                        isCurrent && 'bg-teal/20 text-teal',
                        isPending && 'bg-gray-200 text-gray-400'
                      )}>
                        {isComplete ? (
                          <Check className="w-4 h-4" />
                        ) : (
                          <Icon className="w-4 h-4" />
                        )}
                      </div>
                      <span className={cn(
                        'text-sm font-medium flex-1 transition-all duration-500',
                        isComplete && 'text-teal',
                        isCurrent && 'text-gray-900',
                        isPending && 'text-gray-400'
                      )}>
                        {item.label}
                      </span>
                      {isCurrent && (
                        <LoadingSpinner size="sm" />
                      )}
                      {isComplete && (
                        <Check className="w-4 h-4 text-teal" />
                      )}
                    </div>
                  );
                })}
              </div>

              {/* Progress Bar */}
              <div className="w-full max-w-sm mt-6">
                <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                  <div 
                    className="h-full bg-teal rounded-full transition-all duration-500 ease-out"
                    style={{ width: `${((processingStep + 1) / PROCESSING_STEPS.length) * 100}%` }}
                  />
                </div>
              </div>
            </div>
          )}

          {/* Patient Selection */}
          {step === 'patient' && !isTransitioning && (
            <>
              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  placeholder="Search by name or MRN..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-gray-200 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-teal focus:border-teal"
                  autoFocus
                />
              </div>
              <div className="space-y-2">
                {filteredPatients.map(patient => (
                  <button
                    key={patient.id}
                    onClick={() => handlePatientSelect(patient)}
                    className="w-full flex items-center gap-4 p-4 rounded-xl border border-gray-200 bg-white hover:border-teal hover:bg-teal/5 transition-all text-left click-effect-card"
                  >
                    <div className="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center">
                      <User className="w-5 h-5 text-teal" />
                    </div>
                    <div className="flex-1">
                      <p className="font-semibold text-gray-900">{patient.name}</p>
                      <p className="text-sm text-gray-500">
                        MRN: {patient.mrn} • DOB: {patient.dob} • {patient.payer}
                      </p>
                    </div>
                    <ChevronRight className="w-5 h-5 text-gray-400" />
                  </button>
                ))}
              </div>
            </>
          )}

          {/* Service Selection */}
          {step === 'service' && !isTransitioning && (
            <>
              {/* Selected Patient */}
              <div className="flex items-center gap-3 p-3 rounded-xl bg-teal/5 border border-teal/20 mb-4">
                <div className="w-8 h-8 rounded-lg bg-teal flex items-center justify-center">
                  <User className="w-4 h-4 text-white" />
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-900">{selectedPatient?.name}</p>
                  <p className="text-xs text-gray-500">MRN: {selectedPatient?.mrn}</p>
                </div>
              </div>

              {/* Service Type Tabs */}
              <div className="flex gap-2 mb-4">
                <button
                  onClick={() => { setServiceType('procedure'); setSearchQuery(''); }}
                  className={cn(
                    'flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors click-effect',
                    serviceType === 'procedure' 
                      ? 'bg-teal text-white' 
                      : 'bg-gray-100 text-gray-600 hover:text-gray-900'
                  )}
                >
                  <FileText className="w-4 h-4" />
                  Procedures
                </button>
                <button
                  onClick={() => { setServiceType('medication'); setSearchQuery(''); }}
                  className={cn(
                    'flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors click-effect',
                    serviceType === 'medication' 
                      ? 'bg-teal text-white' 
                      : 'bg-gray-100 text-gray-600 hover:text-gray-900'
                  )}
                >
                  <Pill className="w-4 h-4" />
                  Medications
                </button>
              </div>

              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  placeholder={`Search ${serviceType}s...`}
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-gray-200 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-teal focus:border-teal"
                  autoFocus
                />
              </div>

              <div className="space-y-2">
                {filteredServices.map(service => (
                  <button
                    key={service.code}
                    onClick={() => handleServiceSelect(service)}
                    className="w-full flex items-center gap-4 p-4 rounded-xl border border-gray-200 bg-white hover:border-teal hover:bg-teal/5 transition-all text-left click-effect-card"
                  >
                    <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center">
                      {serviceType === 'procedure' 
                        ? <FileText className="w-5 h-5 text-gray-500" />
                        : <Pill className="w-5 h-5 text-gray-500" />
                      }
                    </div>
                    <div className="flex-1">
                      <p className="font-semibold text-gray-900">{service.name}</p>
                      <p className="text-sm text-gray-500">
                        Code: {service.code}
                        {'dosage' in service && ` • ${service.dosage}`}
                      </p>
                    </div>
                    {service.requiresPA && (
                      <span className="px-2 py-1 rounded-md bg-amber-50 text-amber-600 text-xs font-medium">
                        PA Required
                      </span>
                    )}
                    <ChevronRight className="w-5 h-5 text-gray-400" />
                  </button>
                ))}
              </div>
            </>
          )}

          {/* Diagnosis Selection */}
          {step === 'diagnosis' && !isTransitioning && (
            <>
              {/* Selected Patient & Service */}
              <div className="space-y-2 mb-4">
                <div className="flex items-center gap-3 p-3 rounded-xl bg-teal/5 border border-teal/20">
                  <div className="w-8 h-8 rounded-lg bg-teal flex items-center justify-center">
                    <User className="w-4 h-4 text-white" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-900">{selectedPatient?.name}</p>
                    <p className="text-xs text-gray-500">MRN: {selectedPatient?.mrn}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3 p-3 rounded-xl bg-gray-50 border border-gray-200">
                  <div className="w-8 h-8 rounded-lg bg-gray-200 flex items-center justify-center">
                    <FileText className="w-4 h-4 text-gray-500" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-900">{selectedService?.name}</p>
                    <p className="text-xs text-gray-500">Code: {selectedService?.code}</p>
                  </div>
                </div>
              </div>

              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  placeholder="Search diagnosis by name or ICD-10 code..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-gray-200 bg-white text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-teal focus:border-teal"
                  autoFocus
                />
              </div>

              <div className="space-y-2">
                {filteredDiagnoses.map(diagnosis => (
                  <button
                    key={diagnosis.code}
                    onClick={() => handleDiagnosisSelect(diagnosis)}
                    className="w-full flex items-center gap-4 p-4 rounded-xl border border-gray-200 bg-white hover:border-teal hover:bg-teal/5 transition-all text-left click-effect-card"
                  >
                    <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center font-mono text-sm text-gray-500">
                      {diagnosis.code.slice(0, 3)}
                    </div>
                    <div className="flex-1">
                      <p className="font-semibold text-gray-900">{diagnosis.name}</p>
                      <p className="text-sm text-gray-500">ICD-10: {diagnosis.code}</p>
                    </div>
                    <ChevronRight className="w-5 h-5 text-gray-400" />
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        {step !== 'processing' && (
          <div className="flex items-center justify-between p-5 border-t border-gray-200 bg-gray-50">
            <button
              onClick={() => {
                if (step === 'service') transitionToStep('patient', 300);
                if (step === 'diagnosis') transitionToStep('service', 300);
              }}
              className={cn(
                'px-4 py-2 text-sm font-medium rounded-lg transition-colors click-effect',
                (step === 'patient' || isTransitioning) 
                  ? 'text-gray-400 cursor-not-allowed' 
                  : 'text-gray-700 hover:bg-gray-200'
              )}
              disabled={step === 'patient' || isTransitioning}
            >
              {isTransitioning ? 'Loading...' : 'Back'}
            </button>
            <p className="text-sm text-gray-500">
              {!isTransitioning && step === 'patient' && `${filteredPatients.length} patients`}
              {!isTransitioning && step === 'service' && `${filteredServices.length} ${serviceType}s`}
              {!isTransitioning && step === 'diagnosis' && `${filteredDiagnoses.length} diagnoses`}
              {isTransitioning && 'Loading...'}
            </p>
          </div>
        )}
      </div>
    </div>,
    document.body
  );
}
