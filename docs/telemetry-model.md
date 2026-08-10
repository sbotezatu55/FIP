# Telemetry model

## Normalized model

The normalized model is `Fip.Domain.Flights.Telemetry.FlightTelemetryPoint`. It is deliberately independent of OpenSky and contains no JSON or persistence attributes.

| Field | Type | Interpretation in current implementation |
|---|---|---|
| `Timestamp` | `DateTimeOffset` | Source Unix milliseconds converted to a `DateTimeOffset`. |
| `Icao24` | `string` | Aircraft identity copied from the source. |
| `Callsign` | `string?` | Optional callsign; the OpenSky mapper trims surrounding whitespace. |
| `Latitude` | `double?` | Optional latitude copied from the source. |
| `Longitude` | `double?` | Optional longitude copied from the source. |
| `AltitudeFeet` | `double?` | Optional altitude copied into an explicitly unit-labeled property. |
| `GroundSpeedKnots` | `double?` | Optional ground speed copied into an explicitly unit-labeled property. |
| `TrackDegrees` | `double?` | Optional track copied into an explicitly unit-labeled property. |
| `VerticalRateFeetPerMinute` | `double?` | Optional vertical rate copied into an explicitly unit-labeled property. |

## Source separation

The OpenSky DTO retains external source naming such as `GroundSpeed`, `VerticalRate`, and `Altitude`. The Application mapper is responsible for mapping those fields to the normalized domain names. The Domain assembly does not reference `OpenSkyTelemetryPointDto`, OpenSky namespaces, or `System.Text.Json`.

## Nullability

Telemetry values that may be absent in the source remain nullable through both the DTO and normalized model. The mapper does not substitute defaults or infer missing values. `Icao24` is non-nullable and defaults to `string.Empty`; `Timestamp` is non-nullable.

## Units and conversion

The current mapper does not perform numeric unit conversion. It preserves the source values while making the intended normalized units explicit in property names:

```text
OpenSky Altitude       -> AltitudeFeet
OpenSky GroundSpeed    -> GroundSpeedKnots
OpenSky Track          -> TrackDegrees
OpenSky VerticalRate   -> VerticalRateFeetPerMinute
```

The only conversion currently implemented is timestamp conversion from Unix milliseconds to `DateTimeOffset`.
