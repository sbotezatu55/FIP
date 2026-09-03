import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, inject, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { FlightListItem } from '../../models/flight-list-item';
import { FlightsApiService } from '../../services/flights-api.service';
import { FipIconComponent } from '../../../../shared/components/fip-icon/fip-icon.component';

@Component({
  selector: 'app-flight-list-page',
  imports: [DatePipe, DecimalPipe, RouterLink, FipIconComponent],
  templateUrl: './flight-list-page.component.html',
  styleUrl: './flight-list-page.component.scss'
})
export class FlightListPageComponent {
  private readonly flightsApi = inject(FlightsApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  flights: FlightListItem[] = [];
  isLoading = true;
  errorMessage: string | null = null;
  selectedFlight: FlightListItem | null = null;
  isDeleting = false;
  deleteErrorMessage: string | null = null;

  @ViewChild('deleteDialog') private deleteDialog?: ElementRef<HTMLDialogElement>;

  constructor() {
    this.loadFlights();
  }

  loadFlights(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.flightsApi
      .getFlights()
      .pipe(finalize(() => {
        this.isLoading = false;
        this.changeDetector.markForCheck();
      }))
      .subscribe({
        next: (flights) => {
          this.flights = flights;
          this.changeDetector.markForCheck();
        },
        error: () => {
          this.flights = [];
          this.errorMessage = 'Unable to load flights.';
          this.changeDetector.markForCheck();
        }
      });
  }

  requestDelete(flight: FlightListItem): void {
    this.selectedFlight = flight;
    this.deleteErrorMessage = null;
    this.deleteDialog?.nativeElement.showModal();
  }

  cancelDelete(): void {
    this.deleteDialog?.nativeElement.close();
    this.selectedFlight = null;
  }

  confirmDelete(): void {
    if (!this.selectedFlight || this.isDeleting) return;

    const flightId = this.selectedFlight.id;
    this.isDeleting = true;
    this.deleteErrorMessage = null;
    this.flightsApi.deleteFlight(flightId).pipe(finalize(() => {
      this.isDeleting = false;
      this.changeDetector.markForCheck();
    })).subscribe({
      next: () => {
        this.flights = this.flights.filter(flight => flight.id !== flightId);
        this.cancelDelete();
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.deleteErrorMessage = 'Unable to delete this flight.';
        this.changeDetector.markForCheck();
      }
    });
  }

  formatDuration(duration: string): string {
    const parts = duration.split(':').map(Number);
    if (parts.length !== 3 || parts.some(Number.isNaN)) {
      return duration || '—';
    }

    const [hours, minutes, seconds] = parts;
    const values: string[] = [];
    if (hours > 0) values.push(`${hours}h`);
    if (minutes > 0 || hours > 0) values.push(`${minutes}m`);
    if (hours === 0 && minutes === 0) values.push(`${seconds}s`);

    return values.join(' ');
  }
}
