/**
 * CDS Hooks type definitions
 * Based on HL7 CDS Hooks specification
 */

export interface FhirAuthorization {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  scope?: string;
  subject?: string;
}

export interface CdsContext {
  userId?: string;
  patientId: string;
  encounterId?: string;
  draftOrders?: {
    resourceType: string;
    entry?: Array<{
      resource?: {
        resourceType: string;
        id?: string;
        code?: {
          coding?: Array<{
            system?: string;
            code?: string;
            display?: string;
          }>;
        };
      };
    }>;
  };
}

export interface CdsRequest {
  hookInstance: string;
  hook: string;
  fhirServer?: string;
  fhirAuthorization?: FhirAuthorization;
  context: CdsContext;
  prefetch?: Record<string, unknown>;
}

export interface CdsResponse {
  cards: CdsCard[];
}

export interface CdsCard {
  uuid?: string;
  summary: string;
  detail?: string;
  indicator: 'info' | 'warning' | 'critical' | 'hard-stop';
  source: {
    label: string;
    url?: string;
    icon?: string;
  };
  suggestions?: CdsSuggestion[];
  links?: CdsLink[];
}

export interface CdsSuggestion {
  label: string;
  uuid?: string;
  isRecommended?: boolean;
  actions?: CdsAction[];
}

export interface CdsAction {
  type: 'create' | 'update' | 'delete';
  description?: string;
  resource?: unknown;
}

export interface CdsLink {
  label: string;
  url: string;
  type: 'absolute' | 'smart';
  appContext?: string;
}
