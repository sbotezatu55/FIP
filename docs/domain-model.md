# Domain model

## Current model

The Domain project currently contains two concrete domain types: `Flight` in the `Fip.Domain.Flights` namespace and `FlightTelemetryPoint` in the `Fip.Domain.Flights.Telemetry` namespace.

### Flight

`Flight` represents one reconstructed, source-independent aircraft flight. It owns an ordered snapshot of the normalized telemetry points associated with the flight and exposes that collection as `IReadOnlyList<FlightTelemetryPoint>`.

| Property | Type | Current meaning |
|---|---|---|
| `Id` | `Guid` | Aggregate identity inherited from `Entity`. |
| `Icao24` | `string` | Aircraft transponder identity for the reconstructed flight. |
| `Callsign` | `string?` | Optional flight callsign. |
| `StartTime` / `EndTime` | `DateTimeOffset` | Reconstructed flight time bounds. |
| `DepartureLatitude` / `DepartureLongitude` | `double?` | Optional reconstructed departure coordinates. |
| `ArrivalLatitude` / `ArrivalLongitude` | `double?` | Optional reconstructed arrival coordinates. |
| `MaximumAltitudeFeet` | `double?` | Optional maximum observed altitude in feet. |
| `TelemetryPoints` | `IReadOnlyList<FlightTelemetryPoint>` | Normalized telemetry associated with the flight. |
| `Events` | `IReadOnlyList<FlightEvent>` | Events associated with the reconstructed flight. |

`Flight` stores the telemetry sequence supplied at construction time and does not reconstruct, validate, sort, or infer values from telemetry. `FlightReconstructor` performs the initial ordering and basic metadata derivation in the Application layer.

### Flight events

`FlightEvent` is the initial source-independent representation of a detected aviation event. It preserves an event `Type`, `Timestamp`, optional associated `FlightTelemetryPoint`, and optional description. `FlightEventType` currently includes `Takeoff`, `Landing`, `TopOfClimb`, `TopOfDescent`, and `TelemetryGap`. The `Flight` aggregate owns events and exposes them read-only through `Events`; application event detectors produce events without mutating the aggregate automatically.

### Telemetry validation

`TelemetryValidationStatus`, `TelemetryValidationIssue`, and `TelemetryValidationResult` describe the outcome of evaluating one normalized telemetry point. `TelemetryPointValidator` reports objective invalid conditions for latitude, longitude, track, and default timestamps. It also reports broad heuristic suspicious conditions for unusually high altitude, ground speed, or vertical rate. Validation is observational: it does not mutate, remove, repair, or normalize telemetry points.

### Telemetry gaps

`TelemetryGap` records the timestamps before and after a significant telemetry interruption and the elapsed `Duration`. `TelemetryGapDetector` uses an initial 30-second threshold; intervals equal to or below the threshold are continuous, while longer intervals are reported. It sorts an internal copy of telemetry and does not infer or generate missing points.

### Takeoff detection

`TakeoffDetector` is an Application-layer heuristic that may produce a `FlightEvent` of type `Takeoff` when telemetry shows a groundspeed transition followed by sustained altitude gain and climb evidence. It requires pre-transition low-speed observations and continued climb after the candidate, so an already-airborne segment or isolated noisy sample does not produce an event. Detection does not mutate the `Flight` aggregate.

`LandingDetector` is an Application-layer heuristic that may produce a `FlightEvent` of type `Landing` when sustained relative descent is followed by a stabilized altitude and decelerating rollout. It does not use an absolute runway-elevation threshold and rejects trajectories that show a later sustained climb consistent with a go-around.

`IFlightEventDetector` is the common Application abstraction for detectors that may return zero, one, or multiple `FlightEvent` instances. `FlightEventDetectionService` receives all registered detectors through dependency injection, combines their results, and returns them chronologically. It remains a pure detection operation; callers may explicitly associate the returned events with `Flight` through `AddEvent`.

`TopOfDescentDetector` identifies the transition from a final established cruise segment into sustained descent. It uses relative cruise stability, descent duration, altitude loss, negative vertical-rate evidence, continuity checks, and aborted-descent rejection. It does not use an absolute cruise altitude or interpolate across large telemetry gaps.

`TopOfClimbDetector` identifies the first established transition from sustained climb into a stable level-flight segment. It requires relative altitude gain, climb duration, positive climb evidence, level-flight confirmation, and telemetry continuity. Temporary level-offs and isolated noise are rejected; later step climbs are intentionally ignored by the initial first-transition policy.

### Flight phase classification

`FlightPhase` and `FlightPhaseSegment` are source-independent Domain representations of operational phase intervals. `FlightPhaseClassifier` consumes normalized telemetry and available `FlightEvent` anchors, filters invalid telemetry through the existing validator, orders points defensively, and merges adjacent points with the same phase. The initial phases are `Unknown`, `Ground`, `TakeoffRoll`, `InitialClimb`, `Climb`, `Cruise`, `Descent`, `Approach`, and `LandingRoll`. A large telemetry gap marks the following observation as `Unknown`; missing events are handled through conservative local telemetry heuristics. Phase results remain classifier output rather than mutable state on `Flight`.

### FlightTelemetryPoint

`FlightTelemetryPoint` represents a normalized telemetry observation independent of the source that produced it.

| Property | Type | Current meaning |
|---|---|---|
| `Timestamp` | `DateTimeOffset` | Timestamp of the observation with an explicit offset. |
| `Icao24` | `string` | Aircraft identity value. |
| `Callsign` | `string?` | Optional flight callsign. |
| `Latitude` | `double?` | Optional geographic latitude. |
| `Longitude` | `double?` | Optional geographic longitude. |
| `AltitudeFeet` | `double?` | Optional altitude interpreted in feet. |
| `GroundSpeedKnots` | `double?` | Optional ground speed interpreted in knots. |
| `TrackDegrees` | `double?` | Optional track interpreted in degrees. |
| `VerticalRateFeetPerMinute` | `double?` | Optional vertical rate interpreted in feet per minute. |

The properties use `init` accessors. `Icao24` defaults to an empty string; all other potentially absent telemetry values are nullable except `Timestamp`.

### FlightSummary

`FlightSummary` is a calculated, source-independent statistics model for a reconstructed `Flight`. It contains the available telemetry duration, total geometric trajectory distance in nautical miles, maximum altitude, maximum and average groundspeed, maximum climb and descent rates, detected takeoff and landing timestamps, and airborne `FlightTime`. Nullable statistics remain `null` when the corresponding telemetry or event data is unavailable; distance is `0` when fewer than two consecutive usable positions are available. `FlightSummary` is not stored on the `Flight` aggregate; `FlightSummaryCalculator` produces it from the aggregate's telemetry and events, using the reusable `GeoDistanceCalculator` for each consecutive valid-position segment. Invalid or missing positions break a segment and are never bridged.

## Relationships and base types

`Flight` inherits from `Entity`; `FlightTelemetryPoint` does not inherit from `Entity`, does not implement a value-object base, and is owned by `Flight` through its read-only telemetry collection. `FlightEvent` is associated with `Flight` through its aggregate-owned event collection. There are no aircraft or other aggregate entities in the current Domain project.

## Invariants and rules currently implemented

No event detection or phase-classification behavior is implemented on `FlightEvent` or `Flight`. `Flight` preserves the supplied reconstructed metadata, telemetry association, and explicitly added events. Source-specific conversion currently occurs in the Application mapper, not in Domain.

## Shared kernel

`Fip.SharedKernel.Entity` provides a protected-init `Guid Id` initialized with `Guid.NewGuid()`. `Fip.SharedKernel.ValueObject` is an empty abstract base class. Neither is currently used by `FlightTelemetryPoint`.

## Explicitly not present

The current Domain project has no persistence attributes, EF Core mappings, OpenSky references, JSON attributes, flight reconstruction services, event detection algorithms, or aviation-behavior validation. Its phase types are data models only; classification remains in Application.
