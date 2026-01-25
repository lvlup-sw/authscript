export interface SmartLaunchContext {
  patientId: string;
  encounterId?: string;
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  scope: string;
  fhirBaseUrl: string;
}

export interface SmartConfiguration {
  authorization_endpoint: string;
  token_endpoint: string;
  capabilities: string[];
}

const EPIC_SANDBOX_ISS = 'https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4';

export async function fetchSmartConfiguration(iss: string): Promise<SmartConfiguration> {
  const wellKnownUrl = `${iss}/.well-known/smart-configuration`;
  const response = await fetch(wellKnownUrl);

  if (!response.ok) {
    throw new Error(`Failed to fetch SMART configuration: ${response.statusText}`);
  }

  return response.json();
}

export async function initiateSmartLaunch(
  iss: string,
  launch: string
): Promise<SmartLaunchContext> {
  // In a real implementation, this would:
  // 1. Fetch the SMART configuration
  // 2. Redirect to the authorization endpoint
  // 3. Handle the callback with the auth code
  // 4. Exchange the code for tokens
  // 5. Extract patient/encounter from token response

  // For demo purposes, return mock context
  console.log('SMART Launch initiated', { iss, launch });

  // Simulate network delay
  await new Promise(resolve => setTimeout(resolve, 1000));

  // In production, this would be the actual OAuth flow result
  return {
    patientId: 'demo-001',
    encounterId: 'enc-456',
    accessToken: 'mock-access-token',
    tokenType: 'Bearer',
    expiresIn: 3600,
    scope: 'launch patient/*.read DocumentReference.write',
    fhirBaseUrl: iss || EPIC_SANDBOX_ISS,
  };
}

export function buildAuthorizationUrl(
  config: SmartConfiguration,
  clientId: string,
  redirectUri: string,
  launch: string,
  iss: string
): string {
  const params = new URLSearchParams({
    response_type: 'code',
    client_id: clientId,
    redirect_uri: redirectUri,
    scope: 'launch patient/*.read DocumentReference.write',
    state: crypto.randomUUID(),
    aud: iss,
    launch: launch,
  });

  return `${config.authorization_endpoint}?${params.toString()}`;
}
