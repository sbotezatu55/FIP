# API

## Current status

The API host exposes the first read-only flight query endpoints. Controllers delegate to `Fip.Application`; the API does not access EF Core or `FipDbContext` directly.

`Fip.Api.Program` creates a WebApplication builder, registers Application, Infrastructure, Persistence, and Identity services, registers MVC controllers, maps controller routes, and runs the application.

## Endpoints

### Flights

```http
GET /api/flights
```

Returns `200 OK` with an array of lightweight `FlightListItemDto` records ordered newest first. An empty database returns `[]`. Each record includes `Id`, `Icao24`, `Callsign`, `StartTime`, `EndTime`, `Duration`, `MaximumAltitudeFeet`, departure/arrival coordinates, `TelemetryPointCount`, and `EventCount`; telemetry and event collections are not included.

```http
GET /api/flights/{id}
```

Returns `200 OK` with one lightweight `FlightDetailDto` containing identity, time bounds, duration, departure/arrival coordinates, and maximum altitude, or `404 Not Found` when the ID does not exist. Telemetry and event collections are not included.

```http
GET /api/flights/{id}/summary
```

Returns `200 OK` with a `FlightSummaryDto` containing calculated duration, distance, altitude, groundspeed, vertical-rate, and available takeoff/landing statistics for the selected dashboard flight. Unknown IDs return `404 Not Found`. The response contains numeric/raw values and does not include telemetry or event collections.

```http
GET /api/flights/{id}/telemetry
```

Returns `200 OK` with normalized `FlightTelemetryPointDto` records ordered by `Timestamp` ascending. A known flight with no telemetry returns `[]`; an unknown flight returns `404 Not Found`. Nullable telemetry values remain `null`, and unit-bearing property names are used for altitude, groundspeed, track, and vertical rate. The query projects only telemetry columns and does not load events or the full flight aggregate.

```http
GET /api/flights/{id}/events
```

Returns `200 OK` with stored `FlightEventDto` records ordered by event timestamp ascending. Event types are serialized as stable names such as `Takeoff`, `Landing`, and `TelemetryGap`; existing event coordinates, altitude, and descriptions are included when available. A known flight with no events returns `[]`; an unknown flight returns `404 Not Found`. The endpoint reads stored events and does not run event detection.

```http
POST /api/flights/import
Content-Type: multipart/form-data
```

Accepts one OpenSky JSON trajectory file in the `File` form field and delegates processing to `ImportFlightTrajectoryService`. A new import returns `201 Created` with the import result and a `Location` pointing to `GET /api/flights/{id}`. A duplicate reconstructed flight is treated as idempotent and returns `200 OK` with `Status: Duplicate` and the existing `FlightId`. Empty, non-JSON, malformed, or unusable trajectories return `400 Bad Request`; warnings on an otherwise valid import remain part of a successful result. The response contains import metadata only, not telemetry.

Successful responses include a `diagnostics` object with `source`, logical `filename`, UTC `importedAtUtc`, `recordsRead`, `recordsRejected`, aggregated `warnings`, and numeric `durationMilliseconds`. Diagnostics are returned by the application workflow but are not persisted as import history in the current persistence design.

The response contains flight identity, time bounds, callsign, and summary location/altitude fields. Telemetry and events are not included.

The file `src/Hosts/Fip.Api/Fip.Api.http` contains example requests for these endpoints. Range filtering, sampling, and downsampling are future extensions for large trajectories. Upload size limits and background import processing remain future concerns.

## Host configuration

Development launch settings define:

- HTTP: `http://localhost:5271`
- HTTPS: `https://localhost:7219`

Application settings configure standard logging and `AllowedHosts` for the API. No authentication, authorization, API versioning, or OpenAPI/Swagger configuration is currently present; the new endpoint is therefore exposed through the existing controller route rather than a generated OpenAPI document.
