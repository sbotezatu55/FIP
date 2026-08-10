export type FlightImportStatus = 'Imported' | 'Duplicate';

export interface FlightImportResult {
  status: FlightImportStatus;
  flightId: string;
  callsign: string | null;
  icao24: string;
  pointsImported: number;
  startTime: string;
  endTime: string;
  eventsDetected: number;
  warnings: string[];
}
