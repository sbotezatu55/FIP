import { DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FlightDetail } from '../../models/flight-detail';
import { FlightSummary } from '../../models/flight-summary';

@Component({
  selector: 'app-flight-summary',
  imports: [DecimalPipe],
  templateUrl: './flight-summary.component.html',
  styleUrl: './flight-summary.component.scss'
})
export class FlightSummaryComponent {
  @Input({ required: true }) flight!: FlightDetail;
  @Input({ required: true }) summary: FlightSummary | null = null;
  @Input({ required: true }) summaryErrorMessage: string | null = null;

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

  formatAltitude(altitude: number | null): string {
    return altitude === null ? '—' : `${altitude.toLocaleString('en-US')} ft`;
  }
}
