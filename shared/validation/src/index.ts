import { z } from 'zod';

// Re-export common schemas
export * from './common';

// Evidence item schema
export const evidenceItemSchema = z.object({
  criterionId: z.string(),
  status: z.enum(['MET', 'NOT_MET', 'UNCLEAR']),
  evidence: z.string(),
  source: z.string(),
  confidence: z.number().min(0).max(1),
});

export type EvidenceItem = z.infer<typeof evidenceItemSchema>;

// PA Form response schema
export const paFormResponseSchema = z.object({
  patientName: z.string(),
  patientDob: z.string(),
  memberId: z.string(),
  diagnosisCodes: z.array(z.string()),
  procedureCode: z.string(),
  clinicalSummary: z.string(),
  supportingEvidence: z.array(evidenceItemSchema),
  recommendation: z.enum(['APPROVE', 'NEED_INFO', 'MANUAL_REVIEW']),
  confidenceScore: z.number().min(0).max(1),
  fieldMappings: z.record(z.string()),
});

export type PAFormResponse = z.infer<typeof paFormResponseSchema>;

// Analysis request schema
export const analysisRequestSchema = z.object({
  patientId: z.string(),
  procedureCode: z.string(),
  encounterId: z.string().optional(),
});

export type AnalysisRequest = z.infer<typeof analysisRequestSchema>;

// Status update schema
export const statusUpdateSchema = z.object({
  transactionId: z.string(),
  step: z.string(),
  message: z.string(),
  progress: z.number().min(0).max(100),
  timestamp: z.string(),
  status: z.enum(['pending', 'in_progress', 'completed', 'error']),
});

export type StatusUpdate = z.infer<typeof statusUpdateSchema>;
