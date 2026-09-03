import { HttpClient, HttpEvent, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FlightDetail } from '../models/flight-detail';
import { FlightListItem } from '../models/flight-list-item';
import { FlightSummary } from '../models/flight-summary';
import { FlightTelemetryPoint } from '../models/flight-telemetry-point';
import { FlightEvent } from '../models/flight-event';
import { FlightImportPreviewResult, FlightImportResult } from '../models/flight-import-result';
import { FlightReprocessResult } from '../models/flight-reprocess-result';

@Injectable({ providedIn: 'root' })
export class FlightsApiService {
  private readonly http = inject(HttpClient);

  getFlights(): Observable<FlightListItem[]> {
    return this.http.get<FlightListItem[]>('/api/flights');
  }

  getFlight(id: string): Observable<FlightDetail> {
    return this.http.get<FlightDetail>(`/api/flights/${id}`);
  }

  deleteFlight(id: string): Observable<void> {
    return this.http.delete<void>(`/api/flights/${id}`);
  }

  getFlightSummary(id: string): Observable<FlightSummary> {
    return this.http.get<FlightSummary>(`/api/flights/${id}/summary`);
  }

  getFlightTelemetry(id: string): Observable<FlightTelemetryPoint[]> {
    return this.http.get<FlightTelemetryPoint[]>(`/api/flights/${id}/telemetry`);
  }

  getFlightEvents(id: string): Observable<FlightEvent[]> {
    return this.http.get<FlightEvent[]>(`/api/flights/${id}/events`);
  }

  reprocessFlight(id: string): Observable<FlightReprocessResult> {
    return this.http.post<FlightReprocessResult>(`/api/flights/${id}/reprocess`, {});
  }

  importFlight(file: File): Observable<HttpEvent<FlightImportResult>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<FlightImportResult>('/api/flights/import', formData, {
      observe: 'events',
      reportProgress: true
    });
  }

  previewImport(file: File): Observable<HttpResponse<FlightImportPreviewResult>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<FlightImportPreviewResult>('/api/flights/import/preview', formData, {
      observe: 'response'
    });
  }

  importPreviewCandidate(previewId: string, candidateId: string): Observable<FlightImportResult> {
    return this.http.post<FlightImportResult>(`/api/flights/import/preview/${previewId}/${candidateId}`, {});
  }

  ignorePreviewCandidate(previewId: string, candidateId: string): Observable<void> {
    return this.http.delete<void>(`/api/flights/import/preview/${previewId}/${candidateId}`);
  }
}
