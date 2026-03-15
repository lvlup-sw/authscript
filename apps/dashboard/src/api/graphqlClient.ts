/**
 * GraphQL client for AuthScript Gateway API
 * Uses relative /api/graphql path — proxied to Gateway by Vite (dev) or nginx (prod).
 */

import { GraphQLClient } from 'graphql-request';

const GRAPHQL_ENDPOINT = `${window.location.origin}/api/graphql`;

export const graphqlClient = new GraphQLClient(GRAPHQL_ENDPOINT, {
  credentials: 'include',
  headers: (): HeadersInit => {
    const token = sessionStorage.getItem('authscript_session');
    if (token) {
      try {
        const parsed = JSON.parse(token) as { access_token?: string };
        if (parsed.access_token) {
          return { Authorization: `Bearer ${parsed.access_token}` };
        }
      } catch {
        // ignore invalid JSON
      }
    }
    return {};
  },
});
