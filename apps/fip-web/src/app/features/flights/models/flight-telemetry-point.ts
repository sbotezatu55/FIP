export interface FlightTelemetryPoint {
  timestamp: string;
  latitude: number | null;
  longitude: number | null;
  altitudeFeet: number | null;
  groundSpeedKnots: number | null;
  trackDegrees: number | null;
  verticalRateFeetPerMinute: number | null;
}
