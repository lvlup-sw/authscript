/**
 * Integration tests for GraphQL API service
 * Run with Gateway at http://localhost:5000
 * Skip if backend unavailable: npm run test:run -- --run graphqlService.integration
 */

import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { GraphQLClient, gql } from 'graphql-request';

const GRAPHQL_URL = 'http://localhost:5000/api/graphql';
const client = new GraphQLClient(GRAPHQL_URL);

let backendAvailable = false;
const createdPAIds: string[] = [];

beforeAll(async () => {
  try {
    const res = await fetch(GRAPHQL_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ query: '{ __typename }' }),
    });
    backendAvailable = res.ok;
  } catch {
    backendAvailable = false;
  }
});

async function deletePARequest(id: string): Promise<void> {
  const mutation = gql`
    mutation DeletePARequest($id: String!) {
      deletePARequest(id: $id)
    }
  `;
  await client.request<{ deletePARequest: boolean }>(mutation, { id });
}

describe('GraphQL API Integration', () => {
  afterAll(async () => {
    if (!backendAvailable) return;
    for (const id of createdPAIds) {
      try {
        await deletePARequest(id);
      } catch {
        // ignore cleanup errors
      }
    }
  });

  it('paRequests: returns PA request list', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query {
        paRequests {
          id
          patientId
          procedureCode
          status
          confidence
          patient {
            name
            mrn
          }
        }
      }
    `;
    const data = await client.request<{ paRequests: unknown[] }>(query);
    expect(data.paRequests).toBeDefined();
    expect(Array.isArray(data.paRequests)).toBe(true);
    if (data.paRequests.length > 0) {
      const first = data.paRequests[0] as Record<string, unknown>;
      expect(first).toHaveProperty('id');
      expect(first).toHaveProperty('status');
      expect(first).toHaveProperty('patient');
      expect((first.patient as Record<string, unknown>).name).toBeDefined();
    }
  });

  it('paRequest(id): returns single PA request', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query GetPARequest($id: String!) {
        paRequest(id: $id) {
          id
          procedureCode
          status
          patient {
            name
          }
        }
      }
    `;
    const data = await client.request<{ paRequest: Record<string, unknown> | null }>(query, {
      id: 'PA-001',
    });
    expect(data.paRequest).toBeDefined();
    if (data.paRequest) {
      expect(data.paRequest.id).toBe('PA-001');
      expect(data.paRequest.status).toBeDefined();
    }
  });

  it('paStats: returns stats', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query {
        paStats {
          ready
          submitted
          waitingForInsurance
          attention
          total
        }
      }
    `;
    const data = await client.request<{ paStats: Record<string, number> }>(query);
    expect(data.paStats).toBeDefined();
    expect(data.paStats).toMatchObject({
      ready: expect.any(Number),
      submitted: expect.any(Number),
      waitingForInsurance: expect.any(Number),
      attention: expect.any(Number),
      total: expect.any(Number),
    });
  });

  it('activity: returns activity feed', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query {
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
    const data = await client.request<{ activity: unknown[] }>(query);
    expect(data.activity).toBeDefined();
    expect(Array.isArray(data.activity)).toBe(true);
  });

  it('procedures: returns procedure list', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query {
        procedures {
          code
          name
          category
          requiresPA
        }
      }
    `;
    const data = await client.request<{ procedures: unknown[] }>(query);
    expect(data.procedures).toBeDefined();
    expect(Array.isArray(data.procedures)).toBe(true);
    expect(data.procedures.length).toBeGreaterThan(0);
    const first = data.procedures[0] as Record<string, unknown>;
    expect(first.code).toBeDefined();
    expect(first.name).toBeDefined();
  });

  it('medications: returns medication list', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query {
        medications {
          code
          name
          dosage
          category
          requiresPA
        }
      }
    `;
    const data = await client.request<{ medications: unknown[] }>(query);
    expect(data.medications).toBeDefined();
    expect(Array.isArray(data.medications)).toBe(true);
  });

  it('diagnoses: returns diagnosis list', async () => {
    if (!backendAvailable) return;

    const query = gql`
      query {
        diagnoses {
          code
          name
        }
      }
    `;
    const data = await client.request<{ diagnoses: { code: string; name: string }[] }>(query);
    expect(data.diagnoses).toBeDefined();
    expect(Array.isArray(data.diagnoses)).toBe(true);
    expect(data.diagnoses.length).toBeGreaterThan(0);
  });

  it('createPARequest: creates new PA request', async () => {
    if (!backendAvailable) return;

    const mutation = gql`
      mutation CreatePARequest($input: CreatePARequestInput!) {
        createPARequest(input: $input) {
          id
          status
          patient {
            name
          }
        }
      }
    `;
    const input = {
      patient: {
        id: '60182',
        patientId: '60182',
        name: 'Rebecca Sandbox',
        mrn: '60182',
        dob: '09/14/1990',
        memberId: 'ATH60182',
        payer: 'Blue Cross Blue Shield',
        address: '654 Birch Road',
        phone: '(253) 555-0654',
      },
      procedureCode: '97110',
      diagnosisCode: 'M54.5',
      diagnosisName: 'Low Back Pain',
    };
    const data = await client.request<{ createPARequest: Record<string, unknown> }>(mutation, {
      input,
    });
    expect(data.createPARequest).toBeDefined();
    expect(data.createPARequest.id).toBeDefined();
    expect(data.createPARequest.status).toBe('draft');
    expect((data.createPARequest.patient as Record<string, unknown>).name).toBe('Rebecca Sandbox');
    createdPAIds.push(data.createPARequest.id as string);
  });

  it('submitPARequest: submits PA request', async () => {
    if (!backendAvailable) return;

    const createMutation = gql`
      mutation CreatePARequest($input: CreatePARequestInput!) {
        createPARequest(input: $input) {
          id
          status
        }
      }
    `;
    const createInput = {
      patient: {
        id: '60182',
        patientId: '60182',
        name: 'Rebecca Sandbox',
        mrn: '60182',
        dob: '09/14/1990',
        memberId: 'ATH60182',
        payer: 'Blue Cross Blue Shield',
        address: '654 Birch Road',
        phone: '(253) 555-0654',
      },
      procedureCode: '97110',
      diagnosisCode: 'M54.5',
      diagnosisName: 'Low Back Pain',
    };
    const createData = await client.request<{ createPARequest: { id: string; status: string } }>(
      createMutation,
      { input: createInput }
    );
    const paId = createData.createPARequest.id;
    createdPAIds.push(paId);

    const submitMutation = gql`
      mutation SubmitPARequest($id: String!) {
        submitPARequest(id: $id) {
          id
          status
        }
      }
    `;
    const data = await client.request<{ submitPARequest: Record<string, unknown> | null }>(
      submitMutation,
      { id: paId }
    );
    expect(data.submitPARequest).toBeDefined();
    if (data.submitPARequest) {
      expect(data.submitPARequest.status).toBe('waiting_for_insurance');
    }
  });
});
