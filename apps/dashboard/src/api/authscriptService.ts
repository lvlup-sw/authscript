/**
 * AuthScript API Service
 * Centralized API operations with validation and type safety
 */

import { customFetch } from './customFetch';
import { isValidString } from '../utils/validationUtils';
import type { PAFormResponse, EvidenceItem, StatusUpdate } from '@authscript/types';

const API_BASE_URL = '/api';

// Request types
export interface AnalysisRequest {
  patientId: string;
  procedureCode: string;
  encounterId?: string;
}

// Response types (re-export for convenience)
export interface AnalysisResponse extends PAFormResponse {
  transactionId: string;
}

export type { EvidenceItem, StatusUpdate };

/**
 * AuthScript API service with centralized error handling and validation
 */
export const authscriptService = {
  /**
   * Trigger a new prior authorization analysis
   * @param request - Analysis request with patient and procedure info
   * @returns Transaction ID for tracking
   * @throws Error if required fields are missing
   */
  async triggerAnalysis(request: AnalysisRequest): Promise<{ transactionId: string }> {
    // Guard clauses: validate required fields
    if (!isValidString(request.patientId)) {
      throw new Error('patientId is required');
    }
    if (!isValidString(request.procedureCode)) {
      throw new Error('procedureCode is required');
    }

    // Build request payload (only include defined fields)
    const payload: AnalysisRequest = {
      patientId: request.patientId.trim(),
      procedureCode: request.procedureCode.trim(),
    };

    if (request.encounterId && isValidString(request.encounterId)) {
      payload.encounterId = request.encounterId.trim();
    }

    return customFetch<{ transactionId: string }>({
      url: `${API_BASE_URL}/analysis`,
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  /**
   * Get the result of a completed analysis
   * @param transactionId - Transaction ID from triggerAnalysis
   * @returns Full analysis result with evidence and form data
   * @throws Error if transactionId is missing
   */
  async getAnalysisResult(transactionId: string): Promise<AnalysisResponse> {
    // Guard clause
    if (!isValidString(transactionId)) {
      throw new Error('transactionId is required');
    }

    return customFetch<AnalysisResponse>({
      url: `${API_BASE_URL}/analysis/${encodeURIComponent(transactionId)}`,
    });
  },

  /**
   * Get the current status of an in-progress analysis
   * @param transactionId - Transaction ID from triggerAnalysis
   * @returns Current status with step and progress
   * @throws Error if transactionId is missing
   */
  async getAnalysisStatus(transactionId: string): Promise<StatusUpdate> {
    // Guard clause
    if (!isValidString(transactionId)) {
      throw new Error('transactionId is required');
    }

    return customFetch<StatusUpdate>({
      url: `${API_BASE_URL}/analysis/${encodeURIComponent(transactionId)}/status`,
    });
  },

  /**
   * Download the filled PA form as PDF
   * @param transactionId - Transaction ID of completed analysis
   * @returns PDF blob
   * @throws Error if transactionId is missing or download fails
   */
  async downloadFilledForm(transactionId: string): Promise<Blob> {
    // Guard clause
    if (!isValidString(transactionId)) {
      throw new Error('transactionId is required');
    }

    const response = await fetch(
      `${API_BASE_URL}/analysis/${encodeURIComponent(transactionId)}/form`
    );

    if (!response.ok) {
      throw new Error(`Failed to download form: ${response.statusText}`);
    }

    return response.blob();
  },

  /**
   * Submit the completed form to Epic
   * @param transactionId - Transaction ID of completed analysis
   * @returns Document ID of uploaded form
   * @throws Error if transactionId is missing or submission fails
   */
  async submitToEpic(transactionId: string): Promise<{ documentId: string }> {
    // Guard clause
    if (!isValidString(transactionId)) {
      throw new Error('transactionId is required');
    }

    return customFetch<{ documentId: string }>({
      url: `${API_BASE_URL}/analysis/${encodeURIComponent(transactionId)}/submit`,
      method: 'POST',
    });
  },
};

// Default export for backward compatibility
export default authscriptService;
