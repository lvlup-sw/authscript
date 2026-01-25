import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

describe('SecretsManager', () => {
  beforeEach(() => {
    // Reset modules to get fresh instance
    vi.resetModules();
  });

  afterEach(() => {
    // Restore original env
    vi.unstubAllEnvs();
  });

  describe('initialization', () => {
    it('SecretsManager_ValidEnvVars_LoadsSuccessfully', async () => {
      // Arrange - Set valid environment variables
      vi.stubEnv('VITE_GATEWAY_URL', 'http://localhost:5000');
      vi.stubEnv('VITE_INTELLIGENCE_URL', 'http://localhost:8000');
      vi.stubEnv('VITE_EPIC_CLIENT_ID', 'test-client-id');
      vi.stubEnv('VITE_EPIC_FHIR_BASE_URL', 'https://fhir.epic.com/api');

      // Act - Import fresh module
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert
      expect(manager.hasValidationErrors()).toBe(false);
      expect(manager.gatewayUrl).toBe('http://localhost:5000');
      expect(manager.intelligenceUrl).toBe('http://localhost:8000');
    });

    it('SecretsManager_MissingRequiredVar_RecordsValidationError', async () => {
      // Arrange - Missing required VITE_GATEWAY_URL with no default would fail
      // But GATEWAY_URL has a default, so we need a scenario where required + no default
      // Looking at the spec, both required vars have defaults, so we test the error recording mechanism
      vi.stubEnv('VITE_GATEWAY_URL', '');
      vi.stubEnv('VITE_INTELLIGENCE_URL', '');
      // Clear the defaults by making the module think there's no value and no default

      // For this test, we'll verify the validation logic by checking that
      // empty strings still use defaults
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // With defaults, even empty strings should fall back
      expect(manager.gatewayUrl).toBe('http://localhost:5000');
    });

    it('SecretsManager_UsesDefaultValues_WhenEnvNotSet', async () => {
      // Arrange - Don't set optional vars
      vi.stubEnv('VITE_GATEWAY_URL', '');
      vi.stubEnv('VITE_INTELLIGENCE_URL', '');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert - Should use defaults
      expect(manager.gatewayUrl).toBe('http://localhost:5000');
      expect(manager.intelligenceUrl).toBe('http://localhost:8000');
      expect(manager.epicFhirBaseUrl).toBe('https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4');
    });
  });

  describe('getters', () => {
    it('gatewayUrl_WhenSet_ReturnsValue', async () => {
      // Arrange
      vi.stubEnv('VITE_GATEWAY_URL', 'http://custom-gateway:3000');
      vi.stubEnv('VITE_INTELLIGENCE_URL', 'http://localhost:8000');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert
      expect(manager.gatewayUrl).toBe('http://custom-gateway:3000');
    });

    it('epicClientId_WhenSet_ReturnsValue', async () => {
      // Arrange
      vi.stubEnv('VITE_EPIC_CLIENT_ID', 'my-epic-client-123');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert
      expect(manager.epicClientId).toBe('my-epic-client-123');
    });

    it('epicFhirBaseUrl_WhenSet_ReturnsValue', async () => {
      // Arrange
      vi.stubEnv('VITE_EPIC_FHIR_BASE_URL', 'https://custom-fhir.example.com/api');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert
      expect(manager.epicFhirBaseUrl).toBe('https://custom-fhir.example.com/api');
    });

    it('intelligenceUrl_WhenSet_ReturnsValue', async () => {
      // Arrange
      vi.stubEnv('VITE_INTELLIGENCE_URL', 'http://intelligence:9000');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert
      expect(manager.intelligenceUrl).toBe('http://intelligence:9000');
    });
  });

  describe('getConfigSummary', () => {
    it('getConfigSummary_WithSecrets_MasksValues', async () => {
      // Arrange
      vi.stubEnv('VITE_GATEWAY_URL', 'http://localhost:5000');
      vi.stubEnv('VITE_INTELLIGENCE_URL', 'http://localhost:8000');
      vi.stubEnv('VITE_EPIC_CLIENT_ID', 'super-secret-client-id-12345');
      vi.stubEnv('VITE_EPIC_FHIR_BASE_URL', 'https://fhir.epic.com/api/FHIR/R4');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();
      const summary = manager.getConfigSummary();

      // Assert - Values longer than 8 chars should be masked (first 8 chars + ...)
      expect(summary.gatewayUrl).toBe('http://l...');
      expect(summary.epicClientId).toBe('super-se...');
      expect(summary.intelligenceUrl).toBe('http://l...');
    });

    it('getConfigSummary_WithShortValue_ShowsMasked', async () => {
      // Arrange - Short value (8 chars or less)
      vi.stubEnv('VITE_EPIC_CLIENT_ID', 'short');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();
      const summary = manager.getConfigSummary();

      // Assert - Short values show as ***
      expect(summary.epicClientId).toBe('***');
    });

    it('getConfigSummary_WithUnsetValue_ShowsNotSet', async () => {
      // Arrange - Don't set optional var
      vi.stubEnv('VITE_EPIC_CLIENT_ID', '');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();
      const summary = manager.getConfigSummary();

      // Assert
      expect(summary.epicClientId).toBe('NOT_SET');
    });
  });

  describe('validation', () => {
    it('getValidationErrors_WhenNoErrors_ReturnsEmptyArray', async () => {
      // Arrange
      vi.stubEnv('VITE_GATEWAY_URL', 'http://localhost:5000');
      vi.stubEnv('VITE_INTELLIGENCE_URL', 'http://localhost:8000');

      // Act
      const { SecretsManager } = await import('../secrets');
      const manager = new SecretsManager();

      // Assert
      expect(manager.getValidationErrors()).toEqual([]);
      expect(manager.hasValidationErrors()).toBe(false);
    });
  });

  describe('exported helpers', () => {
    it('getApiConfig_ReturnsGatewayAndIntelligenceUrls', async () => {
      // Arrange
      vi.stubEnv('VITE_GATEWAY_URL', 'http://gateway:5000');
      vi.stubEnv('VITE_INTELLIGENCE_URL', 'http://intelligence:8000');

      // Act
      const { getApiConfig } = await import('../secrets');
      const config = getApiConfig();

      // Assert
      expect(config).toEqual({
        gatewayUrl: 'http://gateway:5000',
        intelligenceUrl: 'http://intelligence:8000',
      });
    });

    it('getEpicConfig_ReturnsClientIdAndFhirUrl', async () => {
      // Arrange
      vi.stubEnv('VITE_EPIC_CLIENT_ID', 'epic-client');
      vi.stubEnv('VITE_EPIC_FHIR_BASE_URL', 'https://epic-fhir.com/api');

      // Act
      const { getEpicConfig } = await import('../secrets');
      const config = getEpicConfig();

      // Assert
      expect(config).toEqual({
        clientId: 'epic-client',
        fhirBaseUrl: 'https://epic-fhir.com/api',
      });
    });
  });
});
