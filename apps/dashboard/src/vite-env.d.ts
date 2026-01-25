/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GATEWAY_URL: string;
  readonly VITE_EPIC_CLIENT_ID: string;
  readonly VITE_EPIC_FHIR_BASE_URL: string;
  readonly VITE_INTELLIGENCE_URL: string;
  readonly PROD: boolean;
  readonly DEV: boolean;
  readonly MODE: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
