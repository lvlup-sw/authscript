import { describe, it, expect, beforeEach } from 'vitest';
import { getAuthToken } from '../auth';

describe('getAuthToken', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('should return token from authscript_session when available', () => {
    sessionStorage.setItem('authscript_session', JSON.stringify({ access_token: 'abc123' }));
    expect(getAuthToken()).toBe('abc123');
  });

  it('should fall back to smart_session when authscript_session is missing', () => {
    sessionStorage.setItem('smart_session', JSON.stringify({ accessToken: 'smart-token-456' }));
    expect(getAuthToken()).toBe('smart-token-456');
  });

  it('should prefer authscript_session over smart_session', () => {
    sessionStorage.setItem('authscript_session', JSON.stringify({ access_token: 'primary' }));
    sessionStorage.setItem('smart_session', JSON.stringify({ accessToken: 'fallback' }));
    expect(getAuthToken()).toBe('primary');
  });

  it('should return null when no sessions exist', () => {
    expect(getAuthToken()).toBeNull();
  });

  it('should return null when session contains invalid JSON', () => {
    sessionStorage.setItem('authscript_session', 'not-valid-json');
    expect(getAuthToken()).toBeNull();
  });

  it('should return null when session has no access_token field', () => {
    sessionStorage.setItem('authscript_session', JSON.stringify({ user: 'test' }));
    expect(getAuthToken()).toBeNull();
  });
});
