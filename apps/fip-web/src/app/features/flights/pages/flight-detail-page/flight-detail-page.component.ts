import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { FlightDetail } from '../../models/flight-detail';
import { FlightSummary } from '../../models/flight-summary';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { FlightsApiService } from '../../services/flights-api.service';
import { TrajectoryMapComponent } from '../../components/trajectory-map/trajectory-map.component';
import { AltitudeChartComponent } from '../../components/altitude-chart/altitude-chart.component';
import { GroundspeedChartComponent } from '../../components/groundspeed-chart/groundspeed-chart.component';
import { VerticalRateChartComponent } from '../../components/vertical-rate-chart/vertical-rate-chart.component';
import { FlightEventTimelineComponent } from '../../components/flight-event-timeline/flight-event-timeline.component';
import { FlightEvent } from '../../models/flight-event';

@Component({
  selector: 'app-flight-detail-page',
  imports: [DatePipe, DecimalPipe, RouterLink, TrajectoryMapComponent, AltitudeChartComponent, GroundspeedChartComponent, VerticalRateChartComponent, FlightEventTimelineComponent],
  templateUrl: './flight-detail-page.component.html',
  styleUrl: './flight-detail-page.component.scss'
})
export class FlightDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly flightsApi = inject(FlightsApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  flight: FlightDetail | null = null;
  summary: FlightSummary | null = null;
  flightId: string | null = null;
  isLoading = true;
  notFound = false;
  errorMessage: string | null = null;
  summaryErrorMessage: string | null = null;
  telemetry: FlightTelemetryPoint[] = [];
  telemetryLoading = true;
  telemetryErrorMessage: string | null = null;
  events: FlightEvent[] = [];
  eventsLoading = true;
  eventsErrorMessage: string | null = null;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.flightId = id;

    if (!id || !this.isGuid(id)) {
      this.isLoading = false;
      this.telemetryLoading = false;
      this.eventsLoading = false;
      this.errorMessage = 'Unable to load flight details.';
      return;
    }

    let detailError: HttpErrorResponse | null = null;
    let summaryFailed = false;

    forkJoin({
      detail: this.flightsApi.getFlight(id).pipe(
        catchError((error: HttpErrorResponse) => {
          detailError = error;
          return of(null);
        })
      ),
      summary: this.flightsApi.getFlightSummary(id).pipe(
        catchError(() => {
          summaryFailed = true;
          return of(null);
        })
      )
    }).subscribe({
      next: ({ detail, summary }) => {
        this.flight = detail;
        this.summary = summary;

        if (detailError?.status === 404) {
          this.notFound = true;
        } else if (!detail) {
          this.errorMessage = 'Unable to load flight details.';
        }

        if (summaryFailed && detail) {
          this.summaryErrorMessage = 'Flight summary is unavailable.';
        }

        this.isLoading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load flight details.';
        this.changeDetector.markForCheck();
      }
    });

    this.flightsApi.getFlightTelemetry(id).subscribe({
      next: (telemetry) => {
        this.telemetry = telemetry;
        this.telemetryLoading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.telemetryLoading = false;
        this.telemetryErrorMessage = 'Unable to load flight trajectory.';
        this.changeDetector.markForCheck();
      }
    });

    this.flightsApi.getFlightEvents(id).subscribe({
      next: (events) => {
        this.events = events;
        this.eventsLoading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.eventsLoading = false;
        this.eventsErrorMessage = 'Unable to load flight events.';
        this.changeDetector.markForCheck();
      }
    });
  }

  formatDuration(duration: string | null | undefined): string {
    if (!duration) return '—';

    const parts = duration.split(':').map(Number);
    if (parts.length !== 3 || parts.some(Number.isNaN)) return duration;

    const [hours, minutes, seconds] = parts;
    const values: string[] = [];
    if (hours > 0) values.push(`${hours}h`);
    if (minutes > 0 || hours > 0) values.push(`${minutes}m`);
    if (hours === 0 && minutes === 0) values.push(`${seconds}s`);

    return values.join(' ');
  }

  formatPosition(latitude: number | null, longitude: number | null): string {
    return latitude === null || longitude === null
      ? '—'
      : `${latitude}, ${longitude}`;
  }

  formatAltitude(altitude: number | null): string {
    return altitude === null ? '—' : `${altitude.toLocaleString('en-US')} ft`;
  }

  private isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }
}
