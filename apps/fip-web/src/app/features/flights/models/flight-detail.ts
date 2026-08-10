export interface FlightDetail {
  id: string;
  icao24: string;
  callsign: string | null;
  startTime: string;
  endTime: string;
  duration: string;
  departureLatitude: number | null;
  departureLongitude: number | null;
  arrivalLatitude: number | null;
  arrivalLongitude: number | null;
  maximumAltitudeFeet: number | null;
}
