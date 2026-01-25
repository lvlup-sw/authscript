/**
 * AuthScript-specific type definitions
 * Prior authorization domain types
 */

export interface PAFormResponse {
  patientName: string;
  patientDob: string;
  memberId: string;
  diagnosisCodes: string[];
  procedureCode: string;
  clinicalSummary: string;
  supportingEvidence: EvidenceItem[];
  recommendation: 'APPROVE' | 'NEED_INFO' | 'MANUAL_REVIEW';
  confidenceScore: number;
  fieldMappings: Record<string, string>;
}

export interface EvidenceItem {
  criterionId: string;
  status: 'MET' | 'NOT_MET' | 'UNCLEAR';
  evidence: string;
  source: string;
  confidence: number;
}

export interface StatusUpdate {
  transactionId: string;
  step: string;
  message: string;
  progress: number;
  timestamp: string;
  status: 'pending' | 'in_progress' | 'completed' | 'error';
}
