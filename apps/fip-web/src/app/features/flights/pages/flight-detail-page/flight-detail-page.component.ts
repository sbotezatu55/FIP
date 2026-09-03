import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, inject, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { FlightDetail } from '../../models/flight-detail';
import { FlightSummary } from '../../models/flight-summary';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { FlightEvent } from '../../models/flight-event';
import { FlightsApiService } from '../../services/flights-api.service';
import { FlightEventTimelineComponent } from '../../components/flight-event-timeline/flight-event-timeline.component';
import { FlightInformationComponent } from '../../components/flight-information/flight-information.component';
import { FlightSummaryComponent } from '../../components/flight-summary/flight-summary.component';
import { FlightAnalysisComponent } from '../../components/flight-analysis/flight-analysis.component';
import { FlightProfileSectionComponent } from '../../components/flight-profile-section/flight-profile-section.component';
import { TrajectoryMapComponent } from '../../components/trajectory-map/trajectory-map.component';
import { AltitudeChartComponent } from '../../components/altitude-chart/altitude-chart.component';
import { GroundspeedChartComponent } from '../../components/groundspeed-chart/groundspeed-chart.component';
import { VerticalRateChartComponent } from '../../components/vertical-rate-chart/vertical-rate-chart.component';
import { FipIconComponent } from '../../../../shared/components/fip-icon/fip-icon.component';

@Component({
  selector: 'app-flight-detail-page',
  imports: [RouterLink, FipIconComponent, FlightInformationComponent, FlightSummaryComponent, FlightAnalysisComponent, FlightProfileSectionComponent, FlightEventTimelineComponent, TrajectoryMapComponent, AltitudeChartComponent, GroundspeedChartComponent, VerticalRateChartComponent],
  templateUrl: './flight-detail-page.component.html',
  styleUrl: './flight-detail-page.component.scss'
})
export class FlightDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
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
  isReprocessing = false;
  reprocessErrorMessage: string | null = null;
  reprocessSuccessMessage: string | null = null;
  isDeleting = false;
  deleteErrorMessage: string | null = null;

  @ViewChild('deleteDialog') private deleteDialog?: ElementRef<HTMLDialogElement>;

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
      detail: this.flightsApi.getFlight(id).pipe(catchError((error: HttpErrorResponse) => {
        detailError = error;
        return of(null);
      })),
      summary: this.flightsApi.getFlightSummary(id).pipe(catchError(() => {
        summaryFailed = true;
        return of(null);
      }))
    }).subscribe({
      next: ({ detail, summary }) => {
        this.flight = detail;
        this.summary = summary;
        if (detailError?.status === 404) this.notFound = true;
        else if (!detail) this.errorMessage = 'Unable to load flight details.';
        if (summaryFailed && detail) this.summaryErrorMessage = 'Flight summary is unavailable.';
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

  reprocessFlight(): void {
    if (!this.flightId || this.isReprocessing) return;

    this.isReprocessing = true;
    this.reprocessErrorMessage = null;
    this.reprocessSuccessMessage = null;
    this.changeDetector.markForCheck();

    this.flightsApi.reprocessFlight(this.flightId).subscribe({
      next: (result) => {
        forkJoin({
          summary: this.flightsApi.getFlightSummary(this.flightId!),
          events: this.flightsApi.getFlightEvents(this.flightId!)
        }).subscribe({
          next: ({ summary, events }) => {
            this.summary = summary;
            this.events = events;
            this.eventsLoading = false;
            this.isReprocessing = false;
            this.reprocessSuccessMessage = `Flight recalculated. ${result.eventsDetected} events detected.`;
            this.changeDetector.markForCheck();
          },
          error: () => this.finishReprocessingWithError('Flight was recalculated, but the updated results could not be loaded.')
        });
      },
      error: (error: HttpErrorResponse) => {
        const message = error.error?.message || 'Unable to recalculate this flight.';
        this.finishReprocessingWithError(message);
      }
    });
  }

  requestDelete(): void {
    this.deleteErrorMessage = null;
    this.deleteDialog?.nativeElement.showModal();
  }

  cancelDelete(): void {
    this.deleteDialog?.nativeElement.close();
  }

  confirmDelete(): void {
    if (!this.flightId || this.isDeleting) return;

    this.isDeleting = true;
    this.deleteErrorMessage = null;
    this.flightsApi.deleteFlight(this.flightId).pipe(finalize(() => {
      this.isDeleting = false;
      this.changeDetector.markForCheck();
    })).subscribe({
      next: () => this.router.navigateByUrl('/flights'),
      error: () => {
        this.deleteErrorMessage = 'Unable to delete this flight.';
        this.changeDetector.markForCheck();
      }
    });
  }

  private finishReprocessingWithError(message: string): void {
    this.isReprocessing = false;
    this.reprocessErrorMessage = message;
    this.changeDetector.markForCheck();
  }

  private isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }
}
