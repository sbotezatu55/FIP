import { DatePipe, DecimalPipe, UpperCasePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { FlightListItem } from '../../../flights/models/flight-list-item';
import { FlightsApiService } from '../../../flights/services/flights-api.service';

@Component({
  selector: 'app-recent-flights',
  imports: [DatePipe, DecimalPipe, RouterLink, UpperCasePipe],
  templateUrl: './recent-flights.component.html',
  styleUrl: './recent-flights.component.scss'
})
export class RecentFlightsComponent {
  private readonly flightsApi = inject(FlightsApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  flights: FlightListItem[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor() {
    this.flightsApi.getFlights().pipe(finalize(() => {
      this.isLoading = false;
      this.changeDetector.markForCheck();
    })).subscribe({
      next: (flights) => {
        this.flights = [...flights]
          .sort((left, right) => Date.parse(right.startTime) - Date.parse(left.startTime))
          .slice(0, 4);
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Recent flights are temporarily unavailable.';
        this.changeDetector.markForCheck();
      }
    });
  }

  formatDuration(duration: string): string {
    const parts = duration.split(':').map(Number);
    if (parts.length !== 3 || parts.some(Number.isNaN)) return duration || '—';

    const [hours, minutes, seconds] = parts;
    const values: string[] = [];
    if (hours > 0) values.push(`${hours}h`);
    if (minutes > 0 || hours > 0) values.push(`${minutes}m`);
    if (hours === 0 && minutes === 0) values.push(`${seconds}s`);
    return values.join(' ');
  }
}
