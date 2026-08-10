export interface FlightListItem {
  id: string;
  icao24: string;
  callsign: string | null;
  startTime: string;
  endTime: string;
  duration: string;
  maximumAltitudeFeet: number | null;
  departureLatitude: number | null;
  departureLongitude: number | null;
  arrivalLatitude: number | null;
  arrivalLongitude: number | null;
  telemetryPointCount: number;
  eventCount: number;
}
