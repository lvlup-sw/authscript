# Implementation Plan: Epic FHIR Integration with Dual Authentication

## Source Design

Link: `docs/designs/2026-01-28-epic-fhir-integration.md`

## Summary

- Total tasks: 12
- Parallel groups: 3
- Estimated test count: ~24

## Parallelization Strategy

```
Group A (Sequential): Tasks 001 → 002 → 003 → 004 → 005
Group B (Sequential): Tasks 006 → 007
Group C (Sequential): Tasks 008 → 009 → 010

Groups A, B, C can run in parallel.

Task 011 depends on: A, B, C (integration)
Task 012 depends on: 011 (DI wiring)
```

## Task Breakdown

---

### Task 001: Define EpicAuthOptions configuration record

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `EpicAuthOptions_WithRequiredProperties_BindsCorrectly`
   - File: `Gateway.API.Tests/Configuration/EpicAuthOptionsTests.cs`
   - Expected failure: `EpicAuthOptions` doesn't have `PrivateKeyPath` or `SigningAlgorithm` properties
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Add new properties to EpicAuthOptions
   - File: `Gateway.API/Configuration/EpicFhirOptions.cs` (rename to `EpicAuthOptions.cs`)
   - Add: `PrivateKeyPath`, `SigningAlgorithm` (default RS384)
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up
   - Ensure consistent naming with design
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group A start)

---

### Task 002: Define ITokenAcquisitionStrategy interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `ITokenAcquisitionStrategy_Interface_Exists`
   - File: `Gateway.API.Tests/Contracts/Http/TokenAcquisitionStrategyTests.cs`
   - Expected failure: Interface doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create interface
   - File: `Gateway.API/Contracts/Http/ITokenAcquisitionStrategy.cs`
   - Methods: `AcquireTokenAsync(CancellationToken)`, `CanHandle` property
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group A)

---

### Task 003: Define ITokenStrategyResolver interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `ITokenStrategyResolver_Interface_Exists`
   - File: `Gateway.API.Tests/Contracts/Http/TokenStrategyResolverTests.cs`
   - Expected failure: Interface doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create interface
   - File: `Gateway.API/Contracts/Http/ITokenStrategyResolver.cs`
   - Method: `Resolve()` returns `ITokenAcquisitionStrategy`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 002
**Parallelizable:** Yes (Group A)

---

### Task 004: Define IFhirHttpClientProvider interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `IFhirHttpClientProvider_Interface_Exists`
   - File: `Gateway.API.Tests/Contracts/Http/FhirHttpClientProviderTests.cs`
   - Expected failure: Interface doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create interface
   - File: `Gateway.API/Contracts/Http/IFhirHttpClientProvider.cs`
   - Method: `GetAuthenticatedClientAsync(CancellationToken)`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group A)

---

### Task 005: Implement CdsHookTokenStrategy

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `CdsHookTokenStrategy_CanHandle_ReturnsTrueWhenTokenInContext`
   - File: `Gateway.API.Tests/Services/Http/CdsHookTokenStrategyTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `CdsHookTokenStrategy_CanHandle_ReturnsFalseWhenNoToken`
   - Expected failure: Class doesn't exist

3. [RED] Write test: `CdsHookTokenStrategy_AcquireTokenAsync_ReturnsTokenFromContext`
   - Expected failure: Class doesn't exist

4. [RED] Write test: `CdsHookTokenStrategy_AcquireTokenAsync_ReturnsNullWhenNoContext`
   - Expected failure: Class doesn't exist

5. [GREEN] Implement CdsHookTokenStrategy
   - File: `Gateway.API/Services/Http/CdsHookTokenStrategy.cs`
   - Reads from `IHttpContextAccessor.HttpContext.Items["FhirAccessToken"]`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 002
**Parallelizable:** Yes (Group A end)

---

### Task 006: Implement JwtBackendTokenStrategy - JWT generation

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `JwtBackendTokenStrategy_CanHandle_AlwaysReturnsTrue`
   - File: `Gateway.API.Tests/Services/Http/JwtBackendTokenStrategyTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `JwtBackendTokenStrategy_GenerateClientAssertion_CreatesValidJwt`
   - Expected failure: Method doesn't exist

3. [RED] Write test: `JwtBackendTokenStrategy_GenerateClientAssertion_SignsWithRS384`
   - Expected failure: Method doesn't exist

4. [RED] Write test: `JwtBackendTokenStrategy_GenerateClientAssertion_IncludesRequiredClaims`
   - Claims: iss, sub (both ClientId), aud, jti, exp
   - Expected failure: Method doesn't exist

5. [GREEN] Implement JWT assertion generation
   - File: `Gateway.API/Services/Http/JwtBackendTokenStrategy.cs`
   - Use `System.IdentityModel.Tokens.Jwt` for JWT creation
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 001, Task 002
**Parallelizable:** Yes (Group B start)

---

### Task 007: Implement JwtBackendTokenStrategy - Token exchange and caching

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `JwtBackendTokenStrategy_AcquireTokenAsync_ExchangesJwtForToken`
   - File: `Gateway.API.Tests/Services/Http/JwtBackendTokenStrategyTests.cs`
   - Expected failure: Method returns null or throws
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `JwtBackendTokenStrategy_AcquireTokenAsync_CachesToken`
   - Expected failure: Second call makes HTTP request

3. [RED] Write test: `JwtBackendTokenStrategy_AcquireTokenAsync_ReturnsCachedToken`
   - Expected failure: Caching not implemented

4. [GREEN] Implement token exchange
   - File: `Gateway.API/Services/Http/JwtBackendTokenStrategy.cs`
   - POST JWT to token endpoint, parse response, cache for 55 minutes
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 006
**Parallelizable:** Yes (Group B end)

---

### Task 008: Implement TokenStrategyResolver

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `TokenStrategyResolver_Resolve_ReturnsCdsStrategyWhenTokenPresent`
   - File: `Gateway.API.Tests/Services/Http/TokenStrategyResolverTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `TokenStrategyResolver_Resolve_ReturnsJwtStrategyWhenNoToken`
   - Expected failure: Class doesn't exist

3. [GREEN] Implement TokenStrategyResolver
   - File: `Gateway.API/Services/Http/TokenStrategyResolver.cs`
   - Checks CdsHookTokenStrategy.CanHandle first, falls back to JwtBackendTokenStrategy
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 003, Task 005, Task 006
**Parallelizable:** Yes (Group C start)

---

### Task 009: Implement FhirHttpClientProvider

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `FhirHttpClientProvider_GetAuthenticatedClientAsync_AttachesBearerToken`
   - File: `Gateway.API.Tests/Services/Http/FhirHttpClientProviderTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `FhirHttpClientProvider_GetAuthenticatedClientAsync_UsesHttpClientFactory`
   - Expected failure: Class doesn't exist

3. [RED] Write test: `FhirHttpClientProvider_GetAuthenticatedClientAsync_CallsTokenStrategy`
   - Expected failure: Class doesn't exist

4. [GREEN] Implement FhirHttpClientProvider
   - File: `Gateway.API/Services/Http/FhirHttpClientProvider.cs`
   - Uses IHttpClientFactory, calls resolver, attaches token
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 004, Task 008
**Parallelizable:** Yes (Group C)

---

### Task 010: Implement CdsHookTokenMiddleware

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `CdsHookTokenMiddleware_InvokeAsync_ExtractsTokenFromPayload`
   - File: `Gateway.API.Tests/Middleware/CdsHookTokenMiddlewareTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `CdsHookTokenMiddleware_InvokeAsync_SetsHttpContextItem`
   - Expected failure: Class doesn't exist

3. [RED] Write test: `CdsHookTokenMiddleware_InvokeAsync_SkipsNonCdsRequests`
   - Expected failure: Class doesn't exist

4. [RED] Write test: `CdsHookTokenMiddleware_InvokeAsync_HandlesNullFhirAuthorization`
   - Expected failure: Class doesn't exist

5. [GREEN] Implement CdsHookTokenMiddleware
   - File: `Gateway.API/Middleware/CdsHookTokenMiddleware.cs`
   - Parse JSON body for `fhirAuthorization.access_token`
   - Store in `HttpContext.Items["FhirAccessToken"]`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group C end)

---

### Task 011: Refactor IFhirHttpClient - Remove accessToken parameter

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `FhirHttpClient_ReadAsync_UsesProviderForAuthentication`
   - File: `Gateway.API.Tests/Services/Fhir/FhirHttpClientTests.cs`
   - Expected failure: FhirHttpClient still requires accessToken parameter
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `FhirHttpClient_SearchAsync_UsesProviderForAuthentication`
   - Expected failure: FhirHttpClient still requires accessToken parameter

3. [RED] Write test: `FhirHttpClient_CreateAsync_UsesProviderForAuthentication`
   - Expected failure: FhirHttpClient still requires accessToken parameter

4. [GREEN] Refactor FhirHttpClient
   - File: `Gateway.API/Contracts/IFhirHttpClient.cs`
   - File: `Gateway.API/Services/Fhir/FhirHttpClient.cs`
   - Remove `accessToken` parameter from all methods
   - Inject `IFhirHttpClientProvider`, call `GetAuthenticatedClientAsync()`
   - Run: `dotnet test` - MUST PASS

5. [REFACTOR] Update dependent code
   - Update `FhirClient`, `BaseFhirRepository`, `EpicFhirContext` to remove accessToken threading
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Tasks 001-010 (all previous tasks)
**Parallelizable:** No (integration point)

---

### Task 012: Wire DI registration

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AddEpicFhirServices_RegistersAllServices`
   - File: `Gateway.API.Tests/DependencyExtensionsTests.cs`
   - Expected failure: New services not registered
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AddEpicFhirServices_ConfiguresHttpClient`
   - Expected failure: HttpClient not configured correctly

3. [GREEN] Update DependencyExtensions
   - File: `Gateway.API/DependencyExtensions.cs`
   - Register: Token strategies, resolver, provider, middleware
   - Configure named HttpClient "EpicFhir"
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 011
**Parallelizable:** No (final integration)

---

## Completion Checklist

- [ ] All tests written before implementation
- [ ] All tests pass
- [ ] Code coverage meets standards
- [ ] No accessToken parameter in any FHIR method signature
- [ ] Repositories use composition (sealed classes, no inheritance)
- [ ] Ready for review
