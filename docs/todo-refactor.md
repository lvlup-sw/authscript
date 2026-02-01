  Architecture Clarification

  Current (Athena MVP):
  ────────────────────
  AthenaPollingService
      └─► TokenStrategyResolver
              └─► AthenaTokenStrategy → OAuth token

  FhirClient
      └─► IFhirHttpClient (takes token param)
              └─► HttpClient

  Future (Multi-EHR):
  ───────────────────
  Any Service
      └─► IFhirHttpClientProvider
              └─► TokenStrategyResolver
                      ├─► AthenaTokenStrategy
                      └─► JwtBackendTokenStrategy (Epic)