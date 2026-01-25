/**
 * Centralized secrets and configuration management for AuthScript dashboard.
 * Provides secure loading of environment variables with validation and type-safe access.
 *
 * @module config/secrets
 */

/**
 * Configuration shape for all managed secrets
 */
interface SecretsConfig {
  /** Gateway API URL */
  readonly gatewayUrl: string;
  /** Epic Launchpad client ID */
  readonly epicClientId: string;
  /** Epic FHIR R4 endpoint */
  readonly epicFhirBaseUrl: string;
  /** Intelligence service URL */
  readonly intelligenceUrl: string;
}

/**
 * Mapping definition for environment variable configuration
 */
interface EnvVarMapping {
  /** Environment variable name */
  readonly envVar: string;
  /** Whether this variable is required */
  readonly required: boolean;
  /** Default value when not set */
  readonly defaultValue?: string;
  /** Human-readable description */
  readonly description: string;
}

/**
 * Environment variable mappings for all configuration keys
 */
const ENV_VAR_MAPPINGS: Readonly<Record<keyof SecretsConfig, EnvVarMapping>> = {
  gatewayUrl: {
    envVar: 'VITE_GATEWAY_URL',
    required: true,
    defaultValue: 'http://localhost:5000',
    description: 'Gateway API URL',
  },
  epicClientId: {
    envVar: 'VITE_EPIC_CLIENT_ID',
    required: false,
    description: 'Epic Launchpad client ID',
  },
  epicFhirBaseUrl: {
    envVar: 'VITE_EPIC_FHIR_BASE_URL',
    required: false,
    defaultValue: 'https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4',
    description: 'Epic FHIR R4 endpoint',
  },
  intelligenceUrl: {
    envVar: 'VITE_INTELLIGENCE_URL',
    required: true,
    defaultValue: 'http://localhost:8000',
    description: 'Intelligence service URL',
  },
} as const;

/**
 * Manages environment configuration with validation and secure access.
 * Validates required variables on initialization and provides type-safe getters.
 *
 * @example
 * ```typescript
 * const manager = new SecretsManager();
 * if (manager.hasValidationErrors()) {
 *   console.error(manager.getValidationErrors());
 * }
 * console.log(manager.gatewayUrl);
 * ```
 */
export class SecretsManager {
  private readonly config: Partial<SecretsConfig> = {};
  private initialized = false;
  private readonly validationErrors: string[] = [];

  /**
   * Creates a new SecretsManager and loads configuration from environment
   */
  constructor() {
    this.loadConfiguration();
  }

  /**
   * Loads and validates all configuration from environment variables
   */
  private loadConfiguration(): void {
    for (const [key, mapping] of Object.entries(ENV_VAR_MAPPINGS)) {
      const value = import.meta.env[mapping.envVar] as string | undefined;

      if (!value && mapping.required && !mapping.defaultValue) {
        this.validationErrors.push(
          `Missing required environment variable: ${mapping.envVar} (${mapping.description})`
        );
        continue;
      }

      (this.config as Record<string, string>)[key] = value || mapping.defaultValue || '';
    }

    if (this.validationErrors.length > 0) {
      console.error('Configuration validation errors:', this.validationErrors);

      if (import.meta.env.PROD) {
        throw new Error('Configuration validation failed in production environment');
      }
    }

    this.initialized = true;
  }

  /**
   * Ensures the manager has been initialized before accessing config
   * @throws Error if not initialized
   */
  private ensureInitialized(): void {
    if (!this.initialized) {
      throw new Error('SecretsManager not initialized');
    }
  }

  /**
   * Gateway API URL for backend communication
   */
  get gatewayUrl(): string {
    this.ensureInitialized();
    return this.config.gatewayUrl || '';
  }

  /**
   * Epic Launchpad OAuth client ID
   */
  get epicClientId(): string {
    this.ensureInitialized();
    return this.config.epicClientId || '';
  }

  /**
   * Epic FHIR R4 base URL for API calls
   */
  get epicFhirBaseUrl(): string {
    this.ensureInitialized();
    return this.config.epicFhirBaseUrl || '';
  }

  /**
   * Intelligence service URL for AI/ML features
   */
  get intelligenceUrl(): string {
    this.ensureInitialized();
    return this.config.intelligenceUrl || '';
  }

  /**
   * Checks if any validation errors occurred during initialization
   * @returns true if there are validation errors
   */
  hasValidationErrors(): boolean {
    return this.validationErrors.length > 0;
  }

  /**
   * Gets all validation errors that occurred during initialization
   * @returns Copy of validation error messages
   */
  getValidationErrors(): readonly string[] {
    return [...this.validationErrors];
  }

  /**
   * Gets a summary of all configuration with sensitive values masked.
   * Useful for logging and debugging without exposing secrets.
   *
   * @returns Record of config keys to masked values
   */
  getConfigSummary(): Record<string, string> {
    this.ensureInitialized();
    const summary: Record<string, string> = {};

    for (const key of Object.keys(ENV_VAR_MAPPINGS)) {
      const value = (this.config as Record<string, string>)[key];
      if (value) {
        // Mask sensitive values for logging
        summary[key] = value.length > 8 ? `${value.slice(0, 8)}...` : '***';
      } else {
        summary[key] = 'NOT_SET';
      }
    }

    return summary;
  }
}

/** Singleton instance for application-wide use */
export const secrets = new SecretsManager();

/**
 * Gets API configuration for backend services
 * @returns Gateway and intelligence service URLs
 */
export const getApiConfig = (): { gatewayUrl: string; intelligenceUrl: string } => ({
  gatewayUrl: secrets.gatewayUrl,
  intelligenceUrl: secrets.intelligenceUrl,
});

/**
 * Gets Epic-specific configuration
 * @returns Epic client ID and FHIR base URL
 */
export const getEpicConfig = (): { clientId: string; fhirBaseUrl: string } => ({
  clientId: secrets.epicClientId,
  fhirBaseUrl: secrets.epicFhirBaseUrl,
});
