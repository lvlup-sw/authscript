/**
 * Get the current authentication token from session storage.
 * Checks authscript_session first, falls back to smart_session (SMART on FHIR).
 */
export function getAuthToken(): string | null {
  // Primary: AuthScript session (uses access_token)
  const authSession = sessionStorage.getItem('authscript_session');
  if (authSession) {
    try {
      const parsed = JSON.parse(authSession) as { access_token?: string };
      if (parsed.access_token) return parsed.access_token;
    } catch {
      // Invalid JSON, fall through
    }
  }

  // Fallback: SMART on FHIR session (uses accessToken — camelCase)
  const smartSession = sessionStorage.getItem('smart_session');
  if (smartSession) {
    try {
      const parsed = JSON.parse(smartSession) as { accessToken?: string };
      if (parsed.accessToken) return parsed.accessToken;
    } catch {
      // Invalid JSON
    }
  }

  return null;
}
