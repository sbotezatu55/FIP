export interface FlightSummary {
  flightId: string;
  callsign: string | null;
  icao24: string;
  startTime: string;
  endTime: string;
  duration: string;
  maximumAltitudeFeet: number | null;
  maximumGroundSpeedKnots: number | null;
  averageGroundSpeedKnots: number | null;
  maximumVerticalRateFeetPerMinute: number | null;
  minimumVerticalRateFeetPerMinute: number | null;
  distanceTraveledNauticalMiles: number;
  takeoffTime: string | null;
  landingTime: string | null;
  flightTime: string | null;
}
