import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';

export interface AltitudeChartPoint {
  x: number;
  y: number;
  timestamp: string;
}

export interface GroundspeedChartPoint {
  x: number;
  y: number;
  timestamp: string;
}

export interface VerticalRateChartPoint {
  x: number;
  y: number;
  timestamp: string;
}

export function toAltitudeChartPoints(points: readonly FlightTelemetryPoint[]): AltitudeChartPoint[] {
  const usablePoints = points
    .filter((point) => {
      const timestamp = Date.parse(point.timestamp);
      return Number.isFinite(timestamp) && point.altitudeFeet !== null && Number.isFinite(point.altitudeFeet);
    })
    .map((point) => ({
      timestamp: point.timestamp,
      timestampMilliseconds: Date.parse(point.timestamp),
      altitudeFeet: point.altitudeFeet as number
    }))
    .sort((left, right) => left.timestampMilliseconds - right.timestampMilliseconds);

  if (usablePoints.length === 0) return [];

  const startTime = usablePoints[0].timestampMilliseconds;
  return usablePoints.map((point) => ({
    x: (point.timestampMilliseconds - startTime) / 1000,
    y: point.altitudeFeet,
    timestamp: point.timestamp
  }));
}

export function toGroundspeedChartPoints(points: readonly FlightTelemetryPoint[]): GroundspeedChartPoint[] {
  const usablePoints = points
    .filter((point) => {
      const timestamp = Date.parse(point.timestamp);
      return Number.isFinite(timestamp) && point.groundSpeedKnots !== null && Number.isFinite(point.groundSpeedKnots);
    })
    .map((point) => ({
      timestamp: point.timestamp,
      timestampMilliseconds: Date.parse(point.timestamp),
      groundSpeedKnots: point.groundSpeedKnots as number
    }))
    .sort((left, right) => left.timestampMilliseconds - right.timestampMilliseconds);

  if (usablePoints.length === 0) return [];

  const startTime = usablePoints[0].timestampMilliseconds;
  return usablePoints.map((point) => ({
    x: (point.timestampMilliseconds - startTime) / 1000,
    y: point.groundSpeedKnots,
    timestamp: point.timestamp
  }));
}

export function toVerticalRateChartPoints(points: readonly FlightTelemetryPoint[]): VerticalRateChartPoint[] {
  const usablePoints = points
    .filter((point) => {
      const timestamp = Date.parse(point.timestamp);
      return Number.isFinite(timestamp) &&
        point.verticalRateFeetPerMinute !== null &&
        Number.isFinite(point.verticalRateFeetPerMinute);
    })
    .map((point) => ({
      timestamp: point.timestamp,
      timestampMilliseconds: Date.parse(point.timestamp),
      verticalRateFeetPerMinute: point.verticalRateFeetPerMinute as number
    }))
    .sort((left, right) => left.timestampMilliseconds - right.timestampMilliseconds);

  if (usablePoints.length === 0) return [];

  const startTime = usablePoints[0].timestampMilliseconds;
  return usablePoints.map((point) => ({
    x: (point.timestampMilliseconds - startTime) / 1000,
    y: point.verticalRateFeetPerMinute,
    timestamp: point.timestamp
  }));
}

export function formatElapsedSeconds(seconds: number, startTimestamp: string): string {
  const timestamp = new Date(Date.parse(startTimestamp) + seconds * 1000);
  return timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

export function formatTelemetryTimestamp(timestamp: string): string {
  return new Date(timestamp).toLocaleString([], {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
}
