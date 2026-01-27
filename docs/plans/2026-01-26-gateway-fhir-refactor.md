# Implementation Plan: Gateway.API FHIR Infrastructure Refactor

**Design:** [2026-01-26-gateway-fhir-refactor.md](../designs/2026-01-26-gateway-fhir-refactor.md)
**Date:** 2026-01-26

## Overview

TDD implementation plan for refactoring Gateway.API to use aegis-api patterns: Result<T> error handling, Hl7.Fhir serialization, IHttpClientProvider with resilience.

## Task Groups

### Group A: Abstractions (Sequential - Foundation)
Tasks 001-004 must complete before other groups can start.

### Group B: Configuration (Parallel-Safe)
Tasks 005-007 can run in parallel after Group A.

### Group C: FHIR Serialization (Parallel-Safe)
Tasks 008-009 can run in parallel after Group A.

### Group D: HTTP Infrastructure (Sequential)
Tasks 010-012 sequential, after Group A.

### Group E: Service Migration (Sequential)
Tasks 013-018 sequential, after Groups B, C, D complete.

### Group F: Integration & Cleanup (Sequential)
Tasks 019-020 after Group E.

---

## Tasks

### Task 001: Result<T> Type
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** A (Foundation)

1. [RED] Write test: `Result_Success_ContainsValue`
   - File: `Gateway.API.Tests/Abstractions/ResultTests.cs`
   - Test that `Result<T>.Success(value)` sets `IsSuccess=true` and `Value=value`

2. [RED] Write test: `Result_Failure_ContainsError`
   - Test that `Result<T>.Failure(error)` sets `IsFailure=true` and `Error=error`

3. [RED] Write test: `Result_Match_ExecutesCorrectBranch`
   - Test that `Match()` executes `onSuccess` for success and `onFailure` for failure

4. [RED] Write test: `Result_Map_TransformsSuccessValue`
   - Test that `Map()` transforms success value and propagates failure

5. [GREEN] Implement `Result<T>`
   - File: `Gateway.API/Abstractions/Result.cs`

6. [REFACTOR] Ensure single public type per file

**Dependencies:** None
**Parallelizable:** No (foundation)

---

### Task 002: Error Record
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** A (Foundation)

1. [RED] Write test: `Error_Constructor_SetsAllProperties`
   - File: `Gateway.API.Tests/Abstractions/ErrorTests.cs`
   - Test Error record with Code, Message, Type, Inner

2. [GREEN] Implement `Error` and `ErrorType`
   - File: `Gateway.API/Abstractions/Error.cs`
   - File: `Gateway.API/Abstractions/ErrorType.cs`

3. [REFACTOR] Verify enum values match HTTP status codes

**Dependencies:** None
**Parallelizable:** No (foundation, but can parallel with 001)

---

### Task 003: ErrorFactory
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** A (Foundation)

1. [RED] Write test: `ErrorFactory_NotFound_ReturnsCorrectError`
   - File: `Gateway.API.Tests/Abstractions/ErrorFactoryTests.cs`
   - Test `ErrorFactory.NotFound("Patient", "123")` returns proper error

2. [RED] Write test: `ErrorFactory_AllMethods_ReturnCorrectErrorType`
   - Test Validation, Unauthorized, Infrastructure, Unexpected

3. [GREEN] Implement `ErrorFactory`
   - File: `Gateway.API/Abstractions/ErrorFactory.cs`

**Dependencies:** 002
**Parallelizable:** No

---

### Task 004: FhirErrors
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** A (Foundation)

1. [RED] Write test: `FhirErrors_StaticErrors_HaveCorrectCodes`
   - File: `Gateway.API.Tests/Errors/FhirErrorsTests.cs`
   - Test ServiceUnavailable, Timeout, AuthenticationFailed have correct codes

2. [RED] Write test: `FhirErrors_FactoryMethods_ReturnCorrectErrors`
   - Test NotFound, InvalidResponse, NetworkError factories

3. [GREEN] Implement `FhirErrors`
   - File: `Gateway.API/Errors/FhirErrors.cs`

**Dependencies:** 003
**Parallelizable:** No

---

### Task 005: EpicFhirOptions
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** B (Configuration)

1. [RED] Write test: `EpicFhirOptions_Binding_LoadsFromConfiguration`
   - File: `Gateway.API.Tests/Configuration/EpicFhirOptionsTests.cs`
   - Test IOptions<EpicFhirOptions> binds from IConfiguration

2. [GREEN] Implement `EpicFhirOptions`
   - File: `Gateway.API/Configuration/EpicFhirOptions.cs`

**Dependencies:** 001-004 (Group A complete)
**Parallelizable:** Yes (with 006, 007)

---

### Task 006: IntelligenceOptions
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** B (Configuration)

1. [RED] Write test: `IntelligenceOptions_Binding_LoadsFromConfiguration`
   - File: `Gateway.API.Tests/Configuration/IntelligenceOptionsTests.cs`
   - Test default TimeoutSeconds = 30

2. [GREEN] Implement `IntelligenceOptions`
   - File: `Gateway.API/Configuration/IntelligenceOptions.cs`

**Dependencies:** 001-004 (Group A complete)
**Parallelizable:** Yes (with 005, 007)

---

### Task 007: ResiliencyOptions
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** B (Configuration)

1. [RED] Write test: `ResiliencyOptions_Defaults_HaveReasonableValues`
   - File: `Gateway.API.Tests/Configuration/ResiliencyOptionsTests.cs`
   - Test default MaxRetryAttempts=3, TimeoutSeconds=10, etc.

2. [GREEN] Implement `ResiliencyOptions`
   - File: `Gateway.API/Configuration/ResiliencyOptions.cs`

**Dependencies:** 001-004 (Group A complete)
**Parallelizable:** Yes (with 005, 006)

---

### Task 008: IFhirSerializer Interface
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** C (FHIR Serialization)

1. [RED] Write test: `FhirSerializer_Serialize_ProducesValidJson`
   - File: `Gateway.API.Tests/Services/Fhir/FhirSerializerTests.cs`
   - Create Patient resource, serialize, verify JSON contains resourceType

2. [RED] Write test: `FhirSerializer_Deserialize_ParsesValidResource`
   - Test deserializing a valid Patient JSON string

3. [RED] Write test: `FhirSerializer_DeserializeBundle_ExtractsEntries`
   - Test deserializing a Bundle with entries

4. [RED] Write test: `FhirSerializer_Deserialize_InvalidJson_ReturnsNull`
   - Test graceful handling of invalid JSON

5. [GREEN] Implement interface and implementation
   - File: `Gateway.API/Contracts/Fhir/IFhirSerializer.cs`
   - File: `Gateway.API/Services/Fhir/FhirSerializer.cs`

**Dependencies:** 001-004 (Group A complete)
**Parallelizable:** Yes (with Group B)

---

### Task 009: Update EpicFhirContext to Use IFhirSerializer
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** C (FHIR Serialization)

1. [RED] Write test: `EpicFhirContext_ReadAsync_UsesFhirSerializer`
   - File: `Gateway.API.Tests/Services/Fhir/EpicFhirContextTests.cs`
   - Mock IFhirSerializer, verify Deserialize called

2. [RED] Write test: `EpicFhirContext_SearchAsync_UsesBundleDeserialization`
   - Test that SearchAsync uses DeserializeBundle

3. [GREEN] Update `EpicFhirContext<T>` constructor and methods
   - File: `Gateway.API/Services/Fhir/EpicFhirContext.cs`
   - Inject IFhirSerializer, replace System.Text.Json calls

4. [REFACTOR] Remove old JsonSerializer/JsonElement usage

**Dependencies:** 008
**Parallelizable:** No (depends on 008)

---

### Task 010: IHttpClientProvider Interface
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** D (HTTP Infrastructure)

1. [RED] Write test: `HttpClientProvider_NoTokenEndpoint_ReturnsUnauthenticatedClient`
   - File: `Gateway.API.Tests/Services/Http/HttpClientProviderTests.cs`
   - Test that missing TokenEndpoint returns client without Authorization header

2. [GREEN] Implement interface
   - File: `Gateway.API/Contracts/Http/IHttpClientProvider.cs`

**Dependencies:** 005 (EpicFhirOptions)
**Parallelizable:** No

---

### Task 011: HttpClientProvider Implementation
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** D (HTTP Infrastructure)

1. [RED] Write test: `HttpClientProvider_WithTokenEndpoint_AcquiresToken`
   - Mock token endpoint response, verify Authorization header set

2. [RED] Write test: `HttpClientProvider_CachesToken_UntilExpiry`
   - Test that subsequent calls use cached token

3. [RED] Write test: `HttpClientProvider_TokenExpired_RefreshesToken`
   - Test token refresh after expiry

4. [RED] Write test: `HttpClientProvider_TokenAcquisitionFails_ReturnsNull`
   - Test graceful failure handling

5. [GREEN] Implement `HttpClientProvider`
   - File: `Gateway.API/Services/Http/HttpClientProvider.cs`

**Dependencies:** 010
**Parallelizable:** No

---

### Task 012: ServiceCollectionExtensions with Resilience
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** D (HTTP Infrastructure)

1. [RED] Write test: `AddEpicFhirClient_RegistersHttpClientWithResilience`
   - File: `Gateway.API.Tests/Extensions/ServiceCollectionExtensionsTests.cs`
   - Verify named HttpClient "EpicFhir" is registered

2. [RED] Write test: `AddIntelligenceClient_RegistersHttpClientWithResilience`
   - Verify named HttpClient "Intelligence" is registered

3. [GREEN] Implement `ServiceCollectionExtensions`
   - File: `Gateway.API/Extensions/ServiceCollectionExtensions.cs`
   - Use AddStandardResilienceHandler from Microsoft.Extensions.Http.Resilience

**Dependencies:** 005, 006, 007, 011
**Parallelizable:** No

---

### Task 013: Update IEpicFhirClient Interface
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** E (Service Migration)

1. [RED] Update existing tests to expect `Result<T>` returns
   - File: `Gateway.API.Tests/Services/EpicFhirClientTests.cs` (create if needed)
   - Test GetPatientAsync returns `Result<PatientInfo>`
   - Test failure cases return `Result.Failure(error)`

2. [GREEN] Update interface
   - File: `Gateway.API/Contracts/IEpicFhirClient.cs`
   - Change all returns to `Result<T>`, remove accessToken parameter

**Dependencies:** 004, 009, 011
**Parallelizable:** No

---

### Task 014: Update EpicFhirClient Implementation
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** E (Service Migration)

1. [RED] Write test: `EpicFhirClient_GetPatientAsync_Success_ReturnsPatientInfo`
   - Mock HttpClient response with valid Patient JSON
   - Verify Result.IsSuccess and Value populated

2. [RED] Write test: `EpicFhirClient_GetPatientAsync_NotFound_ReturnsFailure`
   - Mock 404 response, verify Result.IsFailure with NotFound error

3. [RED] Write test: `EpicFhirClient_GetPatientAsync_NetworkError_ReturnsFailure`
   - Mock HttpRequestException, verify Infrastructure error

4. [RED] Write tests for SearchConditionsAsync, SearchObservationsAsync, etc.
   - Similar pattern for each method

5. [GREEN] Rewrite `EpicFhirClient`
   - File: `Gateway.API/Services/EpicFhirClient.cs`
   - Inject IHttpClientProvider, IFhirSerializer
   - Return Result<T> from all methods
   - Use FHIR model types for parsing, map to info DTOs

**Dependencies:** 013
**Parallelizable:** No

---

### Task 015: Update IFhirDataAggregator and Implementation
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** E (Service Migration)

1. [RED] Write test: `FhirDataAggregator_AllCallsSucceed_ReturnsClinicalBundle`
   - File: `Gateway.API.Tests/Services/FhirDataAggregatorTests.cs`
   - Mock all IEpicFhirClient methods returning success
   - Verify aggregated Result<ClinicalBundle>

2. [RED] Write test: `FhirDataAggregator_PatientFails_ReturnsFailure`
   - Test that patient fetch failure propagates

3. [RED] Write test: `FhirDataAggregator_PartialFailure_IncludesSuccessfulData`
   - Test that conditions failure doesn't block observations

4. [GREEN] Update interface and implementation
   - File: `Gateway.API/Contracts/IFhirDataAggregator.cs`
   - File: `Gateway.API/Services/FhirDataAggregator.cs`
   - Return `Result<ClinicalBundle>`
   - Remove accessToken parameter (use IHttpClientProvider)

**Dependencies:** 014
**Parallelizable:** No

---

### Task 016: Update IIntelligenceClient and Implementation
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** E (Service Migration)

1. [RED] Write test: `IntelligenceClient_AnalyzeAsync_Success_ReturnsPAFormData`
   - File: `Gateway.API.Tests/Services/IntelligenceClientTests.cs`
   - Mock successful /analyze response

2. [RED] Write test: `IntelligenceClient_AnalyzeAsync_HttpError_ReturnsFailure`
   - Mock 500 response, verify Infrastructure error

3. [RED] Write test: `IntelligenceClient_AnalyzeAsync_InvalidResponse_ReturnsFailure`
   - Mock malformed JSON response

4. [GREEN] Update interface and implementation
   - File: `Gateway.API/Contracts/IIntelligenceClient.cs`
   - File: `Gateway.API/Services/IntelligenceClient.cs`
   - Return `Result<PAFormData>`
   - Use registered HttpClient from IHttpClientFactory

**Dependencies:** 012
**Parallelizable:** Yes (with 015, after 012)

---

### Task 017: Update IEpicUploader and Implementation
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** E (Service Migration)

1. [RED] Write test: `EpicUploader_UploadDocumentAsync_Success_ReturnsDocumentId`
   - File: `Gateway.API.Tests/Services/EpicUploaderTests.cs`
   - Mock successful FHIR POST response

2. [RED] Write test: `EpicUploader_UploadDocumentAsync_Unauthorized_ReturnsFailure`
   - Mock 401 response

3. [GREEN] Update interface and implementation
   - File: `Gateway.API/Contracts/IEpicUploader.cs`
   - File: `Gateway.API/Services/EpicUploader.cs`
   - Return `Result<string>`
   - Use IHttpClientProvider, IFhirSerializer

**Dependencies:** 011, 008
**Parallelizable:** Yes (with 015, 016)

---

### Task 018: Update Endpoint Handlers for Result<T>
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** E (Service Migration)

1. [RED] Update test: `AnalysisEndpointsTests` to use Result<T> mocks
   - File: `Gateway.API.Tests/Endpoints/AnalysisEndpointsTests.cs`
   - Update mock returns to use Result<T>
   - Verify Result.Match() used for HTTP response mapping

2. [GREEN] Update endpoint handlers
   - File: `Gateway.API/Endpoints/AnalysisEndpoints.cs`
   - File: `Gateway.API/Endpoints/CdsHooksEndpoints.cs` (if applicable)
   - Use Result.Match() to map to HTTP responses

**Dependencies:** 015, 016, 017
**Parallelizable:** No

---

### Task 019: Update Program.cs DI Registration
**Phase:** RED → GREEN → REFACTOR
**Parallel Group:** F (Integration)

1. [RED] Write integration test: `Program_ServicesRegistered_CanResolveAllDependencies`
   - File: `Gateway.API.Tests/Integration/DependencyInjectionTests.cs`
   - Build ServiceProvider, resolve key services

2. [GREEN] Update `Program.cs`
   - File: `Gateway.API/Program.cs`
   - Use ServiceCollectionExtensions
   - Register IFhirSerializer, IHttpClientProvider
   - Configure options from appsettings

**Dependencies:** 012, 018
**Parallelizable:** No

---

### Task 020: Delete Old Result.cs and Final Cleanup
**Phase:** REFACTOR
**Parallel Group:** F (Integration)

1. Delete `Gateway.API/Contracts/Result.cs`
2. Update any remaining `using Gateway.API.Contracts;` to include `Gateway.API.Abstractions`
3. Run full test suite
4. Verify build succeeds
5. Verify demo app starts and runs

**Dependencies:** 019
**Parallelizable:** No

---

## Execution Order

```
Phase 1 (Foundation):
  001 → 002 → 003 → 004

Phase 2 (Parallel):
  [005, 006, 007] (Config)
  [008 → 009] (FHIR Serialization)
  [010 → 011 → 012] (HTTP)

Phase 3 (Service Migration):
  013 → 014 → 015
              ↘
  016 ─────────→ 018
              ↗
  017 ─────────

Phase 4 (Integration):
  019 → 020
```

## Worktree Strategy

| Branch | Tasks | Description |
|--------|-------|-------------|
| `feature/001-abstractions` | 001-004 | Foundation types |
| `feature/005-config` | 005-007 | Configuration options |
| `feature/008-fhir-serializer` | 008-009 | FHIR serialization |
| `feature/010-http-provider` | 010-012 | HTTP infrastructure |
| `feature/013-service-migration` | 013-018 | Service updates |
| `feature/019-integration` | 019-020 | Final integration |

## Success Criteria

1. All 20 tasks complete with passing tests
2. Zero nullable return types in service interfaces (all use Result<T>)
3. Zero System.Text.Json usage for FHIR resources
4. All HTTP clients use IHttpClientFactory with resilience
5. Single public type per file in all new files
6. Demo app completes full PA workflow
