import { HttpClient, HttpEvent } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FlightDetail } from '../models/flight-detail';
import { FlightListItem } from '../models/flight-list-item';
import { FlightSummary } from '../models/flight-summary';
import { FlightTelemetryPoint } from '../models/flight-telemetry-point';
import { FlightEvent } from '../models/flight-event';
import { FlightImportResult } from '../models/flight-import-result';

@Injectable({ providedIn: 'root' })
export class FlightsApiService {
  private readonly http = inject(HttpClient);

  getFlights(): Observable<FlightListItem[]> {
    return this.http.get<FlightListItem[]>('/api/flights');
  }

  getFlight(id: string): Observable<FlightDetail> {
    return this.http.get<FlightDetail>(`/api/flights/${id}`);
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

  importFlight(file: File): Observable<HttpEvent<FlightImportResult>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<FlightImportResult>('/api/flights/import', formData, {
      observe: 'events',
      reportProgress: true
    });
  }
}
