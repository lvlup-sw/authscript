# Implementation Plan: athenahealth Gateway MVP (.NET Tasks Only)

## Source Design

Link: `docs/designs/2026-01-29-athenahealth-pivot-mvp.md`

## Summary

- Total tasks: 14
- Parallel groups: 4
- Estimated test count: ~35

## Parallelization Strategy

```
Group A (Sequential): A01 → A02 → A03
  - AthenaOptions configuration and token strategy

Group B (Sequential): B01 → B02 → B03 → B04
  - Encounter polling service and processing queue

Group C (Sequential): C01 → C02 → C03
  - Encounter processor (hydration, PDF generation)

Group D (Sequential): D01 → D02 → D03 → D04
  - SSE notifications and DocumentReference write-back

Groups A, B, C, D can run in parallel.

Integration (Sequential after all groups):
  I01 → I02 (DI wiring and resolver update)
```

## Task Breakdown

---

### Task A01: Define AthenaOptions configuration record

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AthenaOptions_WithRequiredProperties_BindsCorrectly`
   - File: `Gateway.API.Tests/Configuration/AthenaOptionsTests.cs`
   - Expected failure: `AthenaOptions` class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AthenaOptions_Validate_ReturnsFalseWhenClientIdMissing`
   - Expected failure: `IsValid()` method doesn't exist

3. [RED] Write test: `AthenaOptions_Validate_ReturnsFalseWhenTokenEndpointMissing`
   - Expected failure: `IsValid()` method doesn't exist

4. [GREEN] Implement AthenaOptions
   - File: `Gateway.API/Configuration/AthenaOptions.cs`
   - Properties: `ClientId`, `ClientSecret`, `FhirBaseUrl`, `TokenEndpoint`, `PollingIntervalSeconds`, `PracticeId`
   - Method: `IsValid()` validates required properties
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group A start)

---

### Task A02: Implement AthenaTokenStrategy - OAuth client credentials

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AthenaTokenStrategy_CanHandle_ReturnsTrueWhenProviderIsAthena`
   - File: `Gateway.API.Tests/Services/Http/AthenaTokenStrategyTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AthenaTokenStrategy_CanHandle_ReturnsFalseWhenProviderIsNotAthena`
   - Expected failure: Class doesn't exist

3. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_PostsToTokenEndpoint`
   - Expected failure: Class doesn't exist

4. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_SendsClientCredentialsGrant`
   - Expected failure: Method doesn't implement OAuth flow

5. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_ReturnsAccessToken`
   - Expected failure: Method doesn't parse token response

6. [GREEN] Implement AthenaTokenStrategy OAuth flow
   - File: `Gateway.API/Services/Http/AthenaTokenStrategy.cs`
   - Inject `IHttpClientFactory`, `IOptions<AthenaOptions>`
   - POST to token endpoint with `grant_type=client_credentials`
   - Parse JSON response for `access_token`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task A01
**Parallelizable:** Yes (Group A)

---

### Task A03: Implement AthenaTokenStrategy - Token caching

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_CachesToken`
   - File: `Gateway.API.Tests/Services/Http/AthenaTokenStrategyTests.cs`
   - Expected failure: Second call makes HTTP request
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_ReturnsCachedToken`
   - Expected failure: Caching not implemented

3. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_RefreshesExpiredToken`
   - Expected failure: Token expiration not tracked

4. [RED] Write test: `AthenaTokenStrategy_AcquireTokenAsync_CachesForExpiryMinus60Seconds`
   - Expected failure: Cache duration not calculated from `expires_in`

5. [GREEN] Implement token caching
   - File: `Gateway.API/Services/Http/AthenaTokenStrategy.cs`
   - Add private field for cached token and expiry time
   - Parse `expires_in` from response, cache for (duration - 60 seconds)
   - Return cached token if not expired
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task A02
**Parallelizable:** Yes (Group A end)

---

### Task B01: Define IEncounterPollingService interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `IEncounterPollingService_Interface_Exists`
   - File: `Gateway.API.Tests/Contracts/IEncounterPollingServiceTests.cs`
   - Expected failure: Interface doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create interface
   - File: `Gateway.API/Contracts/IEncounterPollingService.cs`
   - Extends: `IHostedService` (marker, no additional methods)
   - Purpose: Indicates a background polling service
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group B start)

---

### Task B02: Implement AthenaPollingService - Encounter detection

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AthenaPollingService_ExecuteAsync_PollsForFinishedEncounters`
   - File: `Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AthenaPollingService_ExecuteAsync_SearchesEncounterWithStatusFinished`
   - Expected failure: Class doesn't call FHIR search

3. [RED] Write test: `AthenaPollingService_ExecuteAsync_FiltersEncountersByDateAfterLastCheck`
   - Expected failure: Date filtering not implemented

4. [RED] Write test: `AthenaPollingService_ExecuteAsync_RespectsPollingInterval`
   - Expected failure: Doesn't delay between polls

5. [GREEN] Implement encounter detection
   - File: `Gateway.API/Services/Polling/AthenaPollingService.cs`
   - Extends `BackgroundService`, implements `IEncounterPollingService`
   - Inject `IFhirClient`, `IOptions<AthenaOptions>`
   - Search: `GET /Encounter?status=finished&date=gt{lastCheck}`
   - Poll at `PollingIntervalSeconds` from config
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task B01
**Parallelizable:** Yes (Group B)

---

### Task B03: Implement AthenaPollingService - Deduplication

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AthenaPollingService_ExecuteAsync_SkipsAlreadyProcessedEncounters`
   - File: `Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: Duplicate encounters are processed
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AthenaPollingService_ExecuteAsync_TracksProcessedEncounterIds`
   - Expected failure: No tracking mechanism

3. [RED] Write test: `AthenaPollingService_ExecuteAsync_PurgesOldEntriesFromTracker`
   - Expected failure: Tracker grows unbounded

4. [GREEN] Implement deduplication
   - File: `Gateway.API/Services/Polling/AthenaPollingService.cs`
   - Add `HashSet<string>` or bounded collection for processed encounter IDs
   - Check before enqueuing, add after enqueue
   - Purge entries older than 24 hours (configurable)
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task B02
**Parallelizable:** Yes (Group B)

---

### Task B04: Implement AthenaPollingService - Processing queue

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AthenaPollingService_ExecuteAsync_EnqueuesEncounterToChannel`
   - File: `Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: No queue integration
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AthenaPollingService_Channel_IsUnboundedSingleConsumer`
   - Expected failure: Channel configuration incorrect

3. [GREEN] Implement processing queue
   - File: `Gateway.API/Services/Polling/AthenaPollingService.cs`
   - Add `Channel<string>` for encounter IDs
   - Expose `ChannelReader<string> Encounters` property
   - Write encounter IDs to channel after detection
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task B03
**Parallelizable:** Yes (Group B end)

---

### Task C01: Define IEncounterProcessor interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `IEncounterProcessor_Interface_Exists`
   - File: `Gateway.API.Tests/Contracts/IEncounterProcessorTests.cs`
   - Expected failure: Interface doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create interface
   - File: `Gateway.API/Contracts/IEncounterProcessor.cs`
   - Method: `Task ProcessEncounterAsync(string encounterId, CancellationToken ct)`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group C start)

---

### Task C02: Implement EncounterProcessor - Hydration

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_HydratesPatientContext`
   - File: `Gateway.API.Tests/Services/EncounterProcessorTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_FetchesConditionsObservationsProcedures`
   - Expected failure: Doesn't call FhirDataAggregator

3. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_SendsBundleToIntelligence`
   - Expected failure: Doesn't call Intelligence service

4. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_ReturnsOnIntelligenceError`
   - Expected failure: Error handling not implemented

5. [GREEN] Implement hydration logic
   - File: `Gateway.API/Services/EncounterProcessor.cs`
   - Inject `IFhirDataAggregator`, `IIntelligenceClient`
   - Call aggregator to build `ClinicalBundle`
   - POST bundle to Intelligence service
   - Handle errors gracefully (log, don't throw)
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task C01
**Parallelizable:** Yes (Group C)

---

### Task C03: Implement EncounterProcessor - PDF generation

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_GeneratesPdfFromFormData`
   - File: `Gateway.API.Tests/Services/EncounterProcessorTests.cs`
   - Expected failure: PDF generation not integrated
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_StoresPdfInResultStore`
   - Expected failure: Result store not updated

3. [RED] Write test: `EncounterProcessor_ProcessEncounterAsync_NotifiesViaChannel`
   - Expected failure: Notification channel not integrated

4. [GREEN] Implement PDF generation flow
   - File: `Gateway.API/Services/EncounterProcessor.cs`
   - Call `IPdfFormStamper.StampAsync(formData)`
   - Store result in `IAnalysisResultStore`
   - Write notification to SSE channel
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task C02
**Parallelizable:** Yes (Group C end)

---

### Task D01: Implement SSE notification hub

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `NotificationHub_WriteAsync_WritesToChannel`
   - File: `Gateway.API.Tests/Services/Notifications/NotificationHubTests.cs`
   - Expected failure: Class doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `NotificationHub_ReadAllAsync_ReturnsNotifications`
   - Expected failure: Read not implemented

3. [RED] Write test: `NotificationHub_Channel_IsUnbounded`
   - Expected failure: Channel configuration incorrect

4. [GREEN] Implement NotificationHub
   - File: `Gateway.API/Services/Notifications/NotificationHub.cs`
   - Interface: `INotificationHub` with `WriteAsync`, `ReadAllAsync`
   - Use `Channel<Notification>` (unbounded, single consumer)
   - Singleton lifetime
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group D start)

---

### Task D02: Add SSE endpoint

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `SseEndpoint_Get_ReturnsTextEventStreamContentType`
   - File: `Gateway.API.Tests/Endpoints/SseEndpointsTests.cs`
   - Expected failure: Endpoint doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `SseEndpoint_Get_SetsCacheControlNoCache`
   - Expected failure: Cache header not set

3. [RED] Write test: `SseEndpoint_Get_StreamsNotifications`
   - Expected failure: Streaming not implemented

4. [GREEN] Implement SSE endpoint
   - File: `Gateway.API/Endpoints/SseEndpoints.cs`
   - `app.MapGet("/api/events", ...)`
   - Set headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`
   - Loop over `INotificationHub.ReadAllAsync()`, write `data: {json}\n\n`
   - Flush after each notification
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task D01
**Parallelizable:** Yes (Group D)

---

### Task D03: Implement DocumentReference write-back

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `DocumentUploader_UploadPdfAsync_CreatesDocumentReference`
   - File: `Gateway.API.Tests/Services/DocumentUploaderTests.cs`
   - Expected failure: `UploadPdfAsync` method doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `DocumentUploader_UploadPdfAsync_EncodesContentAsBase64`
   - Expected failure: Base64 encoding not implemented

3. [RED] Write test: `DocumentUploader_UploadPdfAsync_SetsCorrectContentType`
   - Expected failure: `application/pdf` not set

4. [RED] Write test: `DocumentUploader_UploadPdfAsync_LinksToPatientAndEncounter`
   - Expected failure: References not set

5. [GREEN] Implement PDF upload
   - File: `Gateway.API/Services/DocumentUploader.cs`
   - Add `UploadPdfAsync(byte[] pdf, string patientId, string encounterId, CancellationToken ct)`
   - Create DocumentReference with base64-encoded content
   - Set `contentType: application/pdf`, link subject and context
   - POST via `IFhirHttpClient.CreateAsync`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group D)

---

### Task D04: Add submit endpoint

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `SubmitEndpoint_Post_FetchesPdfFromResultStore`
   - File: `Gateway.API.Tests/Endpoints/SubmitEndpointTests.cs`
   - Expected failure: Endpoint doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `SubmitEndpoint_Post_CallsDocumentUploader`
   - Expected failure: Upload not triggered

3. [RED] Write test: `SubmitEndpoint_Post_Returns404WhenResultNotFound`
   - Expected failure: Error handling not implemented

4. [RED] Write test: `SubmitEndpoint_Post_Returns200OnSuccess`
   - Expected failure: Success response not returned

5. [GREEN] Implement submit endpoint
   - File: `Gateway.API/Endpoints/SubmitEndpoints.cs`
   - `app.MapPost("/api/submit/{transactionId}", ...)`
   - Fetch PDF from `IAnalysisResultStore`
   - Call `IDocumentUploader.UploadPdfAsync`
   - Return 200 on success, 404 if not found
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task D03
**Parallelizable:** Yes (Group D end)

---

### Task I01: Wire athenahealth DI registration

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AddAthenaServices_RegistersAthenaOptions`
   - File: `Gateway.API.Tests/DependencyExtensionsTests.cs`
   - Expected failure: `AddAthenaServices` doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `AddAthenaServices_RegistersAthenaTokenStrategy`
   - Expected failure: Token strategy not registered

3. [RED] Write test: `AddAthenaServices_RegistersPollingService`
   - Expected failure: Polling service not registered

4. [RED] Write test: `AddAthenaServices_RegistersEncounterProcessor`
   - Expected failure: Processor not registered

5. [RED] Write test: `AddAthenaServices_RegistersNotificationHub`
   - Expected failure: Hub not registered

6. [GREEN] Implement DI extension
   - File: `Gateway.API/DependencyExtensions.cs`
   - Add `AddAthenaServices(IServiceCollection, IConfiguration)`
   - Register: `AthenaOptions`, `AthenaTokenStrategy`, `AthenaPollingService`, `EncounterProcessor`, `NotificationHub`
   - Configure HttpClient for athena token endpoint
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Tasks A01-A03, B01-B04, C01-C03, D01-D04
**Parallelizable:** No (integration point)

---

### Task I02: Update TokenStrategyResolver for athenahealth

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `TokenStrategyResolver_Resolve_ReturnsAthenaStrategyWhenProviderIsAthena`
   - File: `Gateway.API.Tests/Services/Http/TokenStrategyResolverTests.cs`
   - Expected failure: Athena strategy not in resolver
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `TokenStrategyResolver_Resolve_ChecksAthenaStrategyCanHandle`
   - Expected failure: Resolver doesn't check athena strategy

3. [GREEN] Update TokenStrategyResolver
   - File: `Gateway.API/Services/Http/TokenStrategyResolver.cs`
   - Inject `AthenaTokenStrategy` alongside existing strategies
   - Check `AthenaTokenStrategy.CanHandle` in resolution chain
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task I01
**Parallelizable:** No (final integration)

---

## Completion Checklist

- [ ] All tests written before implementation
- [ ] All tests pass
- [ ] Code coverage meets standards
- [ ] AthenaTokenStrategy implements OAuth client credentials with caching
- [ ] AthenaPollingService detects finished encounters and deduplicates
- [ ] EncounterProcessor hydrates, calls Intelligence, generates PDF
- [ ] SSE endpoint streams notifications to dashboard
- [ ] DocumentReference write-back uploads PDF to patient chart
- [ ] Ready for review
