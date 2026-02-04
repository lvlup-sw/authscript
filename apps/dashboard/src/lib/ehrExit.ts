/**
 * EHR exit / return URL for "Exit to EHR" when the app was launched from an EHR (e.g. SMART launch).
 * Stored in sessionStorage so the header button can redirect back.
 */
const EHR_RETURN_URL_KEY = 'authscript_ehr_return_url';

export function getEhrReturnUrl(): string | null {
  try {
    return sessionStorage.getItem(EHR_RETURN_URL_KEY);
  } catch {
    return null;
  }
}

export function setEhrReturnUrl(url: string): void {
  try {
    sessionStorage.setItem(EHR_RETURN_URL_KEY, url);
  } catch {
    // ignore
  }
}

export function clearEhrReturnUrl(): void {
  try {
    sessionStorage.removeItem(EHR_RETURN_URL_KEY);
  } catch {
    // ignore
  }
}

/**
 * Exit AuthScript and return to the EHR.
 * If we have a stored return URL (from SMART launch), redirect there.
 * Otherwise try to close the window (works when opened by EHR); if that fails, do nothing.
 */
export function exitToEhr(): void {
  const url = getEhrReturnUrl();
  if (url) {
    window.location.href = url;
    return;
  }
  // When launched from EHR in new tab, closing often works
  window.close();
}
