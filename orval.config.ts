import { defineConfig } from 'orval';

const GATEWAY_OPENAPI_INPUT = './apps/gateway/openapi.json';
const INTELLIGENCE_OPENAPI_INPUT = './apps/intelligence/openapi.json';

export default defineConfig({
  // Gateway API configurations
  gateway: {
    input: { target: GATEWAY_OPENAPI_INPUT },
    output: {
      target: './apps/dashboard/src/api/generated/gateway.ts',
      client: 'react-query',
      mode: 'tags-split',
      httpClient: 'fetch',
      baseUrl: '/api',
      override: {
        mutator: {
          path: './apps/dashboard/src/api/customFetch.ts',
          name: 'customFetch',
        },
        query: {
          useQuery: true,
          useMutation: true,
          useSuspenseQuery: true,
          signal: true,
        },
      },
    },
  },
  gatewayTypes: {
    input: { target: GATEWAY_OPENAPI_INPUT },
    output: {
      target: './shared/types/src/generated/gateway.ts',
      client: 'fetch',
      mode: 'single',
    },
  },
  gatewayZod: {
    input: { target: GATEWAY_OPENAPI_INPUT },
    output: {
      target: './shared/validation/src/generated/gateway.zod.ts',
      client: 'zod',
      mode: 'single',
    },
  },

  // Intelligence API configurations
  intelligence: {
    input: { target: INTELLIGENCE_OPENAPI_INPUT },
    output: {
      target: './apps/dashboard/src/api/generated/intelligence.ts',
      client: 'react-query',
      mode: 'tags-split',
      httpClient: 'fetch',
      baseUrl: 'http://localhost:8000',
      override: {
        query: {
          useQuery: true,
          useMutation: true,
          signal: true,
        },
      },
    },
  },
  intelligenceTypes: {
    input: { target: INTELLIGENCE_OPENAPI_INPUT },
    output: {
      target: './shared/types/src/generated/intelligence.ts',
      client: 'fetch',
      mode: 'single',
    },
  },
});
