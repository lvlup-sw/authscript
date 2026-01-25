import { describe, it, expect } from 'vitest';
import type {
  FhirAuthorization,
  CdsContext,
  CdsRequest,
  CdsResponse,
  CdsCard,
  CdsSuggestion,
  CdsAction,
  CdsLink,
} from '../cds';

describe('CDS Types', () => {
  describe('FhirAuthorization', () => {
    it('FhirAuthorization_WithRequired_HasTokenFields', () => {
      const auth: FhirAuthorization = {
        accessToken: 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...',
        tokenType: 'Bearer',
        expiresIn: 3600,
      };
      expect(auth.tokenType).toBe('Bearer');
      expect(auth.expiresIn).toBe(3600);
    });

    it('FhirAuthorization_WithOptional_HasScopeAndSubject', () => {
      const auth: FhirAuthorization = {
        accessToken: 'token',
        tokenType: 'Bearer',
        expiresIn: 3600,
        scope: 'patient/*.read',
        subject: 'Patient/123',
      };
      expect(auth.scope).toBe('patient/*.read');
      expect(auth.subject).toBe('Patient/123');
    });
  });

  describe('CdsContext', () => {
    it('CdsContext_WithRequired_HasPatientId', () => {
      const context: CdsContext = {
        patientId: 'Patient/123',
      };
      expect(context.patientId).toBe('Patient/123');
    });

    it('CdsContext_WithDraftOrders_HasOrderBundle', () => {
      const context: CdsContext = {
        patientId: 'Patient/123',
        userId: 'Practitioner/456',
        encounterId: 'Encounter/789',
        draftOrders: {
          resourceType: 'Bundle',
          entry: [
            {
              resource: {
                resourceType: 'ServiceRequest',
                id: 'sr-1',
                code: {
                  coding: [
                    {
                      system: 'http://snomed.info/sct',
                      code: '77477000',
                      display: 'CT scan',
                    },
                  ],
                },
              },
            },
          ],
        },
      };
      expect(context.draftOrders?.resourceType).toBe('Bundle');
      expect(context.draftOrders?.entry?.[0]?.resource?.resourceType).toBe('ServiceRequest');
    });
  });

  describe('CdsRequest', () => {
    it('CdsRequest_WithRequired_HasHookInfo', () => {
      const request: CdsRequest = {
        hookInstance: 'uuid-123',
        hook: 'order-select',
        context: {
          patientId: 'Patient/123',
        },
      };
      expect(request.hook).toBe('order-select');
      expect(request.hookInstance).toBe('uuid-123');
    });

    it('CdsRequest_WithOptional_HasFhirServer', () => {
      const request: CdsRequest = {
        hookInstance: 'uuid-123',
        hook: 'order-sign',
        fhirServer: 'https://fhir.example.com/r4',
        fhirAuthorization: {
          accessToken: 'token',
          tokenType: 'Bearer',
          expiresIn: 3600,
        },
        context: {
          patientId: 'Patient/123',
        },
        prefetch: {
          patient: { resourceType: 'Patient', id: '123' },
        },
      };
      expect(request.fhirServer).toBe('https://fhir.example.com/r4');
    });
  });

  describe('CdsResponse', () => {
    it('CdsResponse_WithCards_HasCardArray', () => {
      const response: CdsResponse = {
        cards: [
          {
            summary: 'Prior authorization required',
            indicator: 'warning',
            source: { label: 'AuthScript' },
          },
        ],
      };
      expect(response.cards).toHaveLength(1);
      expect(response.cards[0].indicator).toBe('warning');
    });
  });

  describe('CdsCard', () => {
    it('CdsCard_WithRequired_HasSummaryAndSource', () => {
      const card: CdsCard = {
        summary: 'Prior authorization required',
        indicator: 'warning',
        source: { label: 'AuthScript' },
      };
      expect(card.summary).toBe('Prior authorization required');
      expect(card.source.label).toBe('AuthScript');
    });

    it('CdsCard_Indicator_AcceptsValidValues', () => {
      const info: CdsCard['indicator'] = 'info';
      const warning: CdsCard['indicator'] = 'warning';
      const critical: CdsCard['indicator'] = 'critical';
      const hardStop: CdsCard['indicator'] = 'hard-stop';

      expect(['info', 'warning', 'critical', 'hard-stop']).toContain(info);
      expect(['info', 'warning', 'critical', 'hard-stop']).toContain(warning);
      expect(['info', 'warning', 'critical', 'hard-stop']).toContain(critical);
      expect(['info', 'warning', 'critical', 'hard-stop']).toContain(hardStop);
    });

    it('CdsCard_WithOptional_HasSuggestionsAndLinks', () => {
      const card: CdsCard = {
        uuid: 'card-uuid-123',
        summary: 'Prior authorization required',
        detail: 'Detailed explanation here',
        indicator: 'warning',
        source: {
          label: 'AuthScript',
          url: 'https://authscript.com',
          icon: 'https://authscript.com/icon.png',
        },
        suggestions: [
          {
            label: 'Submit PA request',
            isRecommended: true,
          },
        ],
        links: [
          {
            label: 'View PA form',
            url: 'https://authscript.com/pa/123',
            type: 'absolute',
          },
        ],
      };
      expect(card.suggestions).toHaveLength(1);
      expect(card.links).toHaveLength(1);
    });
  });

  describe('CdsSuggestion', () => {
    it('CdsSuggestion_WithRequired_HasLabel', () => {
      const suggestion: CdsSuggestion = {
        label: 'Submit PA request',
      };
      expect(suggestion.label).toBe('Submit PA request');
    });

    it('CdsSuggestion_WithActions_HasActionArray', () => {
      const suggestion: CdsSuggestion = {
        label: 'Auto-fill PA form',
        uuid: 'suggestion-uuid',
        isRecommended: true,
        actions: [
          {
            type: 'create',
            description: 'Create PA request',
            resource: { resourceType: 'ServiceRequest' },
          },
        ],
      };
      expect(suggestion.actions).toHaveLength(1);
      expect(suggestion.actions?.[0].type).toBe('create');
    });
  });

  describe('CdsAction', () => {
    it('CdsAction_Type_AcceptsValidValues', () => {
      const create: CdsAction['type'] = 'create';
      const update: CdsAction['type'] = 'update';
      const del: CdsAction['type'] = 'delete';

      expect(['create', 'update', 'delete']).toContain(create);
      expect(['create', 'update', 'delete']).toContain(update);
      expect(['create', 'update', 'delete']).toContain(del);
    });
  });

  describe('CdsLink', () => {
    it('CdsLink_WithRequired_HasLabelUrlType', () => {
      const link: CdsLink = {
        label: 'View details',
        url: 'https://example.com',
        type: 'absolute',
      };
      expect(link.label).toBe('View details');
      expect(link.type).toBe('absolute');
    });

    it('CdsLink_Type_AcceptsValidValues', () => {
      const absolute: CdsLink['type'] = 'absolute';
      const smart: CdsLink['type'] = 'smart';

      expect(['absolute', 'smart']).toContain(absolute);
      expect(['absolute', 'smart']).toContain(smart);
    });

    it('CdsLink_Smart_HasAppContext', () => {
      const link: CdsLink = {
        label: 'Launch PA app',
        url: 'https://smart.example.com/launch',
        type: 'smart',
        appContext: 'patient=123&orderId=456',
      };
      expect(link.appContext).toBe('patient=123&orderId=456');
    });
  });
});
