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
  diagnostics: FlightImportDiagnostics;
}

export interface FlightImportDiagnostics {
  source: string;
  filename: string;
  importedAtUtc: string;
  recordsRead: number;
  recordsRejected: number;
  warnings: string[];
  durationMilliseconds: number;
}
