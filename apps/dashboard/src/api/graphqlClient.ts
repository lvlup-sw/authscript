/**
 * GraphQL client for AuthScript Gateway API
 * Uses VITE_GATEWAY_URL (default http://localhost:5000) for direct calls.
 * When using Vite dev proxy, /api is proxied to the Gateway.
 */

import { GraphQLClient } from 'graphql-request';
import { getApiConfig } from '../config/secrets';
import { getAuthToken } from './auth';

const GRAPHQL_ENDPOINT = `${getApiConfig().gatewayUrl}/api/graphql`;

export const graphqlClient = new GraphQLClient(GRAPHQL_ENDPOINT, {
  credentials: 'include',
  headers: (): HeadersInit => {
    const token = getAuthToken();
    if (token) {
      return { Authorization: `Bearer ${token}` };
    }
    return {};
  },
});
