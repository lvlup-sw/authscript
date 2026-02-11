/**
 * GraphQL API service - replaces mock store, communicates with backend
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { graphqlClient } from './graphqlClient';
import { gql } from 'graphql-request';

// Fragments and types
const PATIENT_FRAGMENT = gql`
  fragment PatientFields on PatientModel {
    id
    name
    mrn
    dob
    memberId
    payer
    address
    phone
  }
`;

const CRITERION_FRAGMENT = gql`
  fragment CriterionFields on CriterionModel {
    met
    label
  }
`;

const PA_REQUEST_FRAGMENT = gql`
  fragment PARequestFields on PARequestModel {
    id
    patientId
    patient {
      ...PatientFields
    }
    procedureCode
    procedureName
    diagnosis
    diagnosisCode
    payer
    provider
    providerNpi
    serviceDate
    placeOfService
    clinicalSummary
    status
    confidence
    createdAt
    updatedAt
    readyAt
    submittedAt
    reviewTimeSeconds
    criteria {
      ...CriterionFields
    }
  }
  ${PATIENT_FRAGMENT}
  ${CRITERION_FRAGMENT}
`;

// Queries
export const GET_PA_REQUESTS = gql`
  query GetPARequests {
    paRequests {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

export const GET_PA_REQUEST = gql`
  query GetPARequest($id: String!) {
    paRequest(id: $id) {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

export const GET_PA_STATS = gql`
  query GetPAStats {
    paStats {
      ready
      submitted
      waitingForInsurance
      attention
      total
    }
  }
`;

export const GET_ACTIVITY = gql`
  query GetActivity {
    activity {
      id
      action
      patientName
      procedureCode
      time
      type
    }
  }
`;

export const GET_PROCEDURES = gql`
  query GetProcedures {
    procedures {
      code
      name
      category
      requiresPA
    }
  }
`;

export const GET_MEDICATIONS = gql`
  query GetMedications {
    medications {
      code
      name
      dosage
      category
      requiresPA
    }
  }
`;

export const GET_DIAGNOSES = gql`
  query GetDiagnoses {
    diagnoses {
      code
      name
    }
  }
`;

// Mutations
export const CREATE_PA_REQUEST = gql`
  mutation CreatePARequest($input: CreatePARequestInput!) {
    createPARequest(input: $input) {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

/** Patient input for CreatePARequest (from frontend Athena test patients) */
export interface PatientInput {
  id: string;
  patientId: string;
  fhirId?: string;
  name: string;
  mrn: string;
  dob: string;
  memberId: string;
  payer: string;
  address: string;
  phone: string;
}

export const UPDATE_PA_REQUEST = gql`
  mutation UpdatePARequest($input: UpdatePARequestInput!) {
    updatePARequest(input: $input) {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

export const PROCESS_PA_REQUEST = gql`
  mutation ProcessPARequest($id: String!) {
    processPARequest(id: $id) {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

export const SUBMIT_PA_REQUEST = gql`
  mutation SubmitPARequest($id: String!, $addReviewTimeSeconds: Int) {
    submitPARequest(id: $id, addReviewTimeSeconds: $addReviewTimeSeconds) {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

export const ADD_REVIEW_TIME = gql`
  mutation AddReviewTime($id: String!, $seconds: Int!) {
    addReviewTime(id: $id, seconds: $seconds) {
      ...PARequestFields
    }
  }
  ${PA_REQUEST_FRAGMENT}
`;

// TypeScript types (match backend models)
export interface Patient {
  id: string;
  name: string;
  mrn: string;
  dob: string;
  memberId: string;
  payer: string;
  address: string;
  phone: string;
}

export interface Procedure {
  code: string;
  name: string;
  category: string;
  requiresPA: boolean;
}

export interface Medication {
  code: string;
  name: string;
  dosage: string;
  category: string;
  requiresPA: boolean;
}

export interface Criterion {
  met: boolean | null;
  label: string;
}

export interface PARequest {
  id: string;
  patientId: string;
  patient: Patient;
  procedureCode: string;
  procedureName: string;
  diagnosis: string;
  diagnosisCode: string;
  payer: string;
  provider: string;
  providerNpi: string;
  serviceDate: string;
  placeOfService: string;
  clinicalSummary: string;
  status: 'draft' | 'ready' | 'waiting_for_insurance' | 'approved' | 'denied';
  confidence: number;
  createdAt: string;
  updatedAt: string;
  readyAt?: string | null;
  submittedAt?: string | null;
  reviewTimeSeconds?: number;
  criteria: Criterion[];
}

export interface PAStats {
  ready: number;
  submitted: number;
  waitingForInsurance: number;
  attention: number;
  total: number;
}

export interface ActivityItem {
  id: string;
  action: string;
  patientName: string;
  procedureCode: string;
  time: string;
  type: 'success' | 'ready' | 'info';
}

// Query keys
export const QUERY_KEYS = {
  paRequests: ['paRequests'] as const,
  paRequest: (id: string) => ['paRequest', id] as const,
  paStats: ['paStats'] as const,
  activity: ['activity'] as const,
  procedures: ['procedures'] as const,
  medications: ['medications'] as const,
  diagnoses: ['diagnoses'] as const,
};

// Hooks
export function usePARequests() {
  return useQuery({
    queryKey: QUERY_KEYS.paRequests,
    queryFn: async () => {
      const data = await graphqlClient.request<{ paRequests: PARequest[] }>(GET_PA_REQUESTS);
      return data.paRequests;
    },
    refetchInterval: 5000,
  });
}

export function usePARequest(id: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.paRequest(id ?? ''),
    queryFn: async () => {
      if (!id) return null;
      const data = await graphqlClient.request<{ paRequest: PARequest | null }>(GET_PA_REQUEST, { id });
      return data.paRequest;
    },
    enabled: Boolean(id),
  });
}

export function usePAStats() {
  return useQuery({
    queryKey: QUERY_KEYS.paStats,
    queryFn: async () => {
      const data = await graphqlClient.request<{ paStats: PAStats }>(GET_PA_STATS);
      return data.paStats;
    },
    refetchInterval: 5000,
  });
}

export function useActivity() {
  return useQuery({
    queryKey: QUERY_KEYS.activity,
    queryFn: async () => {
      const data = await graphqlClient.request<{ activity: ActivityItem[] }>(GET_ACTIVITY);
      return data.activity;
    },
    refetchInterval: 5000,
  });
}

export function useProcedures(enabled = true) {
  return useQuery({
    queryKey: QUERY_KEYS.procedures,
    queryFn: async () => {
      const data = await graphqlClient.request<{ procedures: Procedure[] }>(GET_PROCEDURES);
      return data.procedures;
    },
    enabled,
  });
}

export function useMedications(enabled = true) {
  return useQuery({
    queryKey: QUERY_KEYS.medications,
    queryFn: async () => {
      const data = await graphqlClient.request<{ medications: Medication[] }>(GET_MEDICATIONS);
      return data.medications;
    },
    enabled,
  });
}

export function useDiagnoses(enabled = true) {
  return useQuery({
    queryKey: QUERY_KEYS.diagnoses,
    queryFn: async () => {
      const data = await graphqlClient.request<{ diagnoses: { code: string; name: string }[] }>(GET_DIAGNOSES);
      return data.diagnoses;
    },
    enabled,
  });
}

export function useCreatePARequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (input: { patient: PatientInput; procedureCode: string; diagnosisCode: string; diagnosisName: string; providerId?: string }) => {
      const data = await graphqlClient.request<{ createPARequest: PARequest }>(CREATE_PA_REQUEST, {
        input: {
          patient: input.patient,
          procedureCode: input.procedureCode,
          diagnosisCode: input.diagnosisCode,
          diagnosisName: input.diagnosisName,
          providerId: input.providerId ?? 'DR001',
        },
      });
      return data.createPARequest;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequests });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paStats });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.activity });
    },
  });
}

export function useProcessPARequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const data = await graphqlClient.request<{ processPARequest: PARequest | null }>(PROCESS_PA_REQUEST, { id });
      return data.processPARequest;
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequests });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequest(id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paStats });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.activity });
    },
  });
}

export function useUpdatePARequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (input: {
      id: string;
      diagnosis?: string;
      diagnosisCode?: string;
      serviceDate?: string;
      placeOfService?: string;
      clinicalSummary?: string;
      criteria?: { met: boolean | null; label: string }[];
    }) => {
      const data = await graphqlClient.request<{ updatePARequest: PARequest | null }>(UPDATE_PA_REQUEST, {
        input: {
          ...input,
          criteria: input.criteria?.map((c) => ({ met: c.met, label: c.label })),
        },
      });
      return data.updatePARequest;
    },
    onSuccess: (_, input) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequests });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequest(input.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paStats });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.activity });
    },
  });
}

export function useSubmitPARequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { id: string; addReviewTimeSeconds?: number }) => {
      const { id, addReviewTimeSeconds = 0 } = typeof params === 'string' ? { id: params, addReviewTimeSeconds: 0 } : params;
      const data = await graphqlClient.request<{ submitPARequest: PARequest | null }>(SUBMIT_PA_REQUEST, {
        id,
        addReviewTimeSeconds,
      });
      return data.submitPARequest;
    },
    onSuccess: (_, params) => {
      const id = typeof params === 'string' ? params : params.id;
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequests });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequest(id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paStats });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.activity });
    },
  });
}

export function useAddReviewTime() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, seconds }: { id: string; seconds: number }) => {
      const data = await graphqlClient.request<{ addReviewTime: PARequest | null }>(ADD_REVIEW_TIME, { id, seconds });
      return data.addReviewTime;
    },
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequests });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paRequest(id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.paStats });
    },
  });
}
