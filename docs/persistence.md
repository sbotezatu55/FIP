# Persistence

## Current status

The initial persistence model is defined in `Fip.Persistence` using dedicated EF Core entities and `FipDbContext`. The model covers reconstructed flights, normalized telemetry points, and detected flight events. `FlightConfiguration`, `FlightTelemetryPointConfiguration`, and `FlightEventConfiguration` map the three tables and their relationships. The initial migration is `InitialFlightTelemetrySchema`, and `FlightRepository` implements the application-facing flight repository contract.

## Database technology

`Fip.Persistence` uses EF Core 10.0.9 with the SQL Server provider. Runtime registration reads the `ConnectionString-AppDb` connection-string key. Development credentials should be supplied through API user secrets or the `ConnectionStrings__ConnectionString-AppDb` environment variable; credentials are not stored in the repository. Design-time migrations target `DESKTOP-BIRS3VI\\SQLEXPRESS` and database `FIP`, with a safe integrated-security fallback when the environment variable is absent. Repository-local `dotnet-ef` tooling is pinned to 10.0.9.

## Persistence abstractions

`Fip.Application.Abstractions.Persistence` defines the application-facing `IFlightRepository` with:

- `GetByIdAsync(Guid, CancellationToken)` returning `Flight?`.
- `GetAllAsync(CancellationToken)` returning lightweight newest-first `FlightQueryModel` projections with telemetry/event counts.
- `GetSummaryByIdAsync(Guid, CancellationToken)` returning a lightweight `FlightQueryModel?` projection.
- `GetTelemetryAsync(Guid, CancellationToken)` returning a flight-existence flag and normalized telemetry projection ordered by timestamp.
- `GetEventsAsync(Guid, CancellationToken)` returning a flight-existence flag and stored event projections ordered by timestamp.
- `FindExistingFlightIdAsync(string, DateTimeOffset, DateTimeOffset, CancellationToken)` for lightweight duplicate lookup.
- `AddAsync(Flight, CancellationToken)` for newly reconstructed flights.

It also contains contracts for:

- `IAircraftRepository`
- `IFlightRepository`
- `ITelemetryRepository`
- `IUnitOfWork`

`Fip.Persistence.Repositories.FlightRepository` implements the contract with explicit domain/persistence mapping. `GetAllAsync` and `GetSummaryByIdAsync` project only flight summary columns; the list projection uses database-side navigation counts and avoids loading telemetry and event entities. `GetTelemetryAsync` and `GetEventsAsync` check flight existence separately from focused, no-tracking child projections so an existing flight with zero points/events is distinguishable from an unknown flight. `GetEventsAsync` orders by timestamp and event ID for deterministic timeline output. `GetByIdAsync` loads the full aggregate, including telemetry and events, using no-tracking split queries and chronological mapping; `AddAsync` stages the aggregate graph without committing it. `Fip.Persistence.UnitOfWork.UnitOfWork` implements `IUnitOfWork` and delegates the commit boundary to `FipDbContext.SaveChangesAsync`. Repositories and the unit of work are scoped and share the same scoped `FipDbContext`; application services can stage one or more repository operations and then commit them through `IUnitOfWork`. The repository abstraction does not expose EF Core, persistence entities, queryables, tracking flags, or database-specific loading options.

## Entities and relationships

`Fip.Persistence.Entities` contains `FlightEntity`, `FlightTelemetryPointEntity`, and `FlightEventEntity`. `Fip.Persistence.Context.FipDbContext` exposes `Flights`, `FlightTelemetryPoints`, and `FlightEvents`; it does not expose domain models as `DbSet`s.

The relationships are:

- One `FlightEntity` to many `FlightTelemetryPointEntity` records.
- One `FlightEntity` to many `FlightEventEntity` records.
- Every telemetry point and event has a required `FlightId` foreign key and navigation to its parent flight.

The entities use `Guid` primary keys, `DateTimeOffset` timestamps, and nullable `double` values for optional coordinates and telemetry measurements. The event entity stores the optional event telemetry location/altitude as a snapshot because the domain event exposes those values through its optional telemetry point; no separate event-to-telemetry relationship is introduced.

`FlightConfiguration` maps `FlightEntity` to `Flights`, bounds `Icao24` to six characters and callsigns to eight characters, and indexes `Icao24`, `Callsign`, `StartTime`, and `EndTime`. It also defines the non-unique composite index `IX_Flights_Icao24_StartTime_EndTime` for duplicate lookup. The index is intentionally non-unique because future multi-source ingestion may legitimately produce identical reconstructed identity values. Application-generated `Guid` keys are configured with `ValueGeneratedNever`. Flight deletion cascades to telemetry points and events. No relationship is configured for automatic eager loading.

### Duplicate flight identity

Phase 7 duplicate protection uses the reconstructed flight identity:

```text
ICAO24 + StartTime + EndTime
```

The application checks this identity after reconstruction and before event detection, summary calculation, or persistence. The repository projects only the existing flight ID, avoiding telemetry, events, and aggregate loading. A duplicate returns an explicit duplicate import result containing the existing `FlightId`; no new telemetry, events, or commit are performed.

The composite index is an application-performance aid rather than a uniqueness constraint. The application-level check can still race under concurrent imports, so this phase does not claim database-enforced concurrency protection. Timestamp comparison uses the persisted `DateTimeOffset` values exactly; no tolerance is applied.

`FlightTelemetryPointConfiguration` maps telemetry to `FlightTelemetryPoints`, preserves the existing `DateTimeOffset` and nullable `double` values, bounds retained aviation identifiers, and configures the non-unique `FlightId`/`Timestamp` index `IX_FlightTelemetryPoints_FlightId_Timestamp`. The composite index is intentionally the only initial telemetry access index: its `FlightId` prefix supports per-flight retrieval and chronological ordering, while no current global timestamp query justifies a standalone timestamp index. Duplicate timestamps are not ruled out by the current ingestion/domain model. The current API returns complete trajectories; range filtering or downsampling will likely be needed for very large flights later.

`FlightEventConfiguration` maps events to `FlightEvents`, stores `FlightEventType` as a bounded string, preserves optional location/altitude and description values, and configures the non-unique `FlightId`/`Timestamp` index `IX_FlightEvents_FlightId_Timestamp`. No `FlightId`/`Type` uniqueness or automatic eager loading is configured.

## Database migration host

`Fip.DatabaseMigrator` references `Fip.Persistence` but does not apply migrations yet. Migration commands use the repository-local EF tool and the design-time `FipDbContextFactory`.

## Configuration

`Fip.Api` registers `FipDbContext`, the scoped `IFlightRepository`, and the scoped `IUnitOfWork` through `AddPersistence(IConfiguration)`. The development SQL Server Express connection should be stored with API user secrets:

```text
dotnet user-secrets set "ConnectionStrings:ConnectionString-AppDb" "<local SQL Server Express connection string>" --project src/Hosts/Fip.Api/Fip.Api.csproj
```

The equivalent environment variable is `ConnectionStrings__ConnectionString-AppDb`. The repository contains no database password.

The design-time factory reads that environment variable and otherwise uses integrated security against `DESKTOP-BIRS3VI\\SQLEXPRESS`, database `FIP`.

## Initial migration

`Migrations/20260809034530_InitialFlightTelemetrySchema.cs` creates `Flights`, `FlightTelemetryPoints`, and `FlightEvents`. `Migrations/20260809110000_AddFlightDuplicateLookupIndex.cs` adds the non-unique composite flight identity index. The migrations contain no seed data and have reversible `Down` operations. The migrations have not been applied to a database.

## Future boundary

Domain models remain EF-independent, and persistence entities contain no aviation behavior. Advanced indexes, telemetry loading optimization, update/delete workflows, and additional repositories remain future concerns. In particular, telemetry is queried by `FlightId` and chronological `Timestamp`; uniqueness of that pair has not been assumed.
