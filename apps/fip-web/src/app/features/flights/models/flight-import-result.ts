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

export type FlightImportCandidateStatus = 'Complete' | 'PartialStart' | 'PartialEnd' | 'TooShort';

export interface FlightImportCandidate {
  candidateId: string;
  callsign: string | null;
  icao24: string;
  startTime: string;
  endTime: string;
  points: number;
  status: FlightImportCandidateStatus;
}

export interface FlightImportPreviewResult {
  previewId: string;
  candidates: FlightImportCandidate[];
  source: string;
  filename: string;
  recordsRead: number;
  duplicateRecordsRemoved: number;
}
