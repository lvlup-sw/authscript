# Observability, Persistence & Test Coverage

**Date:** 2026-02-02
**Status:** Draft
**Author:** Claude + Reed

## Overview

This design addresses three related concerns for production readiness:

1. **Data Flow Observability** - Verify FHIR data reaches Intelligence service without logging PHI
2. **PostgreSQL Persistence** - Replace in-memory stores with durable storage
3. **Test Coverage CI** - Enforce 60% coverage threshold as PR status check

## Problem Statement

### Observability Gap

The current logging in `FhirDataAggregator` (2 statements) and `EncounterProcessor` (21 statements) doesn't provide visibility into the actual data shape flowing through the system. When testing with athenahealth sandbox patients, we cannot verify that real clinical data is being processed without inspecting PHI.

### Data Loss Risk

`InMemoryWorkItemStore` and `InMemoryPatientRegistry` lose all data on application restart. PostgreSQL and Redis are provisioned but unused for core persistence.

### Coverage Blind Spot

CI runs tests but doesn't enforce coverage thresholds. Quality can regress without visibility.

## Design

### 1. Data Flow Observability

#### Signal Logging Pattern

Add structured logs that indicate data presence/shape without exposing PHI:

```csharp
// FhirDataAggregator - after aggregation
_logger.LogInformation(
    "Aggregated clinical data for patient: Conditions={ConditionCount}, " +
    "Observations={ObservationCount}, Procedures={ProcedureCount}, " +
    "Documents={DocumentCount}, ServiceRequests={ServiceRequestCount}, " +
    "HasPatientDemographics={HasPatient}",
    bundle.Conditions.Count,
    bundle.Observations.Count,
    bundle.Procedures.Count,
    bundle.Documents.Count,
    bundle.ServiceRequests.Count,
    bundle.Patient is not null);

// EncounterProcessor - before Intelligence call
_logger.LogInformation(
    "Sending to Intelligence: ProcedureCode={ProcedureCode}, " +
    "BundleConditions={ConditionCount}, BundleObservations={ObservationCount}",
    procedureCode,
    clinicalBundle.Conditions.Count,
    clinicalBundle.Observations.Count);
```

#### Validation Signals

Add boolean flags for required data presence:

```csharp
var hasRequiredData = bundle.Patient is not null
    && bundle.Conditions.Count > 0
    && !string.IsNullOrEmpty(procedureCode);

_logger.LogInformation(
    "Data validation: HasRequiredData={HasRequiredData}, " +
    "PatientPresent={PatientPresent}, ConditionsPresent={ConditionsPresent}, " +
    "ProcedureCodePresent={ProcedureCodePresent}",
    hasRequiredData,
    bundle.Patient is not null,
    bundle.Conditions.Count > 0,
    !string.IsNullOrEmpty(procedureCode));
```

### 2. PostgreSQL Persistence

#### Technology Choice: EF Core

**Rationale:**
- Native Aspire integration via `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`
- Type-safe migrations
- Familiar pattern for .NET developers
- Supports the existing record types with minimal mapping

#### Database Schema

```sql
-- WorkItems table
CREATE TABLE work_items (
    id VARCHAR(32) PRIMARY KEY,
    patient_id VARCHAR(100) NOT NULL,
    encounter_id VARCHAR(100) NOT NULL,
    service_request_id VARCHAR(100),
    procedure_code VARCHAR(20),
    status INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,

    INDEX idx_work_items_encounter (encounter_id),
    INDEX idx_work_items_status (status)
);

-- RegisteredPatients table
CREATE TABLE registered_patients (
    patient_id VARCHAR(100) PRIMARY KEY,
    encounter_id VARCHAR(100) NOT NULL,
    practice_id VARCHAR(50) NOT NULL,
    work_item_id VARCHAR(32) NOT NULL,
    registered_at TIMESTAMPTZ NOT NULL,
    last_polled_at TIMESTAMPTZ,
    current_encounter_status VARCHAR(50),

    INDEX idx_registered_patients_registered (registered_at)
);
```

#### Implementation Structure

```
Gateway.API/
├── Data/
│   ├── GatewayDbContext.cs
│   ├── Entities/
│   │   ├── WorkItemEntity.cs
│   │   └── RegisteredPatientEntity.cs
│   └── Configurations/
│       ├── WorkItemConfiguration.cs
│       └── RegisteredPatientConfiguration.cs
├── Services/
│   ├── PostgresWorkItemStore.cs      (implements IWorkItemStore)
│   └── PostgresPatientRegistry.cs    (implements IPatientRegistry)
```

#### Entity Mapping

```csharp
// WorkItemEntity.cs
public sealed class WorkItemEntity
{
    public required string Id { get; set; }
    public required string PatientId { get; set; }
    public required string EncounterId { get; set; }
    public string? ServiceRequestId { get; set; }
    public string? ProcedureCode { get; set; }
    public WorkItemStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

// Mapping extension
public static class WorkItemMappings
{
    public static WorkItem ToModel(this WorkItemEntity entity) => new()
    {
        Id = entity.Id,
        PatientId = entity.PatientId,
        // ... etc
    };

    public static WorkItemEntity ToEntity(this WorkItem model) => new()
    {
        Id = model.Id,
        PatientId = model.PatientId,
        // ... etc
    };
}
```

#### DI Registration

```csharp
// DependencyExtensions.cs
public static IServiceCollection AddGatewayPersistence(
    this IServiceCollection services)
{
    services.AddDbContext<GatewayDbContext>();
    services.AddScoped<IWorkItemStore, PostgresWorkItemStore>();
    services.AddScoped<IPatientRegistry, PostgresPatientRegistry>();
    return services;
}
```

#### Migration Strategy

1. Create initial migration with EF Core CLI
2. Apply migrations on startup in development
3. Use explicit migration in production deployments

```csharp
// Program.cs (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
    await db.Database.MigrateAsync();
}
```

### 3. Test Coverage CI

#### Coverage Collection

Add to `Gateway.API.Tests.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.4" />
</ItemGroup>
```

#### CI Configuration

Update `.github/workflows/ci.yml`:

```yaml
gateway-build:
  # ... existing setup ...

  - name: Test with Coverage
    run: |
      dotnet test apps/gateway/Gateway.API.Tests \
        --collect:"XPlat Code Coverage" \
        --results-directory ./coverage \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

  - name: Check Coverage Threshold
    run: |
      COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' ./coverage/*/coverage.cobertura.xml | head -1)
      COVERAGE_PCT=$(echo "$COVERAGE * 100" | bc | cut -d. -f1)
      echo "Coverage: ${COVERAGE_PCT}%"
      if [ "$COVERAGE_PCT" -lt 60 ]; then
        echo "::error::Coverage ${COVERAGE_PCT}% is below 60% threshold"
        exit 1
      fi

  - name: Upload Coverage Report
    uses: actions/upload-artifact@v4
    with:
      name: coverage-report
      path: ./coverage/*/coverage.cobertura.xml
```

#### Branch Protection

Add status check requirement for `gateway-build` job to pass before merge.

## Task Breakdown

### Observability (3 tasks)

| ID | Task | Files |
|----|------|-------|
| OBS-001 | Add signal logging to FhirDataAggregator | `FhirDataAggregator.cs` |
| OBS-002 | Add pre-Intelligence logging to EncounterProcessor | `EncounterProcessor.cs` |
| OBS-003 | Add data validation logging | `EncounterProcessor.cs` |

### Persistence (8 tasks)

| ID | Task | Files |
|----|------|-------|
| PER-001 | Add EF Core packages | `Gateway.API.csproj` |
| PER-002 | Create WorkItemEntity and configuration | `Data/Entities/`, `Data/Configurations/` |
| PER-003 | Create RegisteredPatientEntity and configuration | `Data/Entities/`, `Data/Configurations/` |
| PER-004 | Create GatewayDbContext | `Data/GatewayDbContext.cs` |
| PER-005 | Create initial migration | `Data/Migrations/` |
| PER-006 | Implement PostgresWorkItemStore | `Services/PostgresWorkItemStore.cs` |
| PER-007 | Implement PostgresPatientRegistry | `Services/PostgresPatientRegistry.cs` |
| PER-008 | Update DI registration | `DependencyExtensions.cs`, `Program.cs` |

### Coverage (2 tasks)

| ID | Task | Files |
|----|------|-------|
| COV-001 | Add coverlet.collector package | `Gateway.API.Tests.csproj` |
| COV-002 | Update CI with coverage threshold | `.github/workflows/ci.yml` |

## Redis Caching (Deferred)

Create GitHub issue for future work:

**Title:** Implement Redis caching for SSE notifications and session state

**Body:**
- Replace in-memory notification channel with Redis pub/sub for multi-instance support
- Consider Redis for session affinity in load-balanced deployments
- AnalysisResultStore already uses Redis (no changes needed)

## Testing Strategy

### Unit Tests (TUnit)

- `PostgresWorkItemStoreTests.cs` - Mock DbContext, test CRUD operations
- `PostgresPatientRegistryTests.cs` - Mock DbContext, test registry operations

### Integration Tests (Alba)

- Use Testcontainers for PostgreSQL
- Test full request/response cycle with real database
- Verify data persists across requests

## Acceptance Criteria

1. **Observability**
   - [ ] Logs show bundle counts (Conditions, Observations, etc.) without PHI
   - [ ] Logs show data validation flags (HasRequiredData, etc.)
   - [ ] Can verify real data flows through by inspecting logs

2. **Persistence**
   - [ ] WorkItems persist across application restarts
   - [ ] RegisteredPatients persist across application restarts
   - [ ] Existing API contracts unchanged
   - [ ] All existing tests pass

3. **Coverage**
   - [ ] CI reports coverage percentage
   - [ ] PRs fail if coverage < 60%
   - [ ] Coverage report uploaded as artifact

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| EF Core performance for high throughput | Use async/await, consider read replicas later |
| Migration conflicts | Single migration per PR, squash before merge |
| Testcontainers CI time | Cache container images, parallel test execution |

## Open Questions

1. Should we support both in-memory and PostgreSQL via configuration for local dev?
2. Do we need soft-delete for audit trail?
