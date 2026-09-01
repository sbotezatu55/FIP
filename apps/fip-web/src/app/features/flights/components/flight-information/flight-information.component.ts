import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FlightDetail } from '../../models/flight-detail';
import { FipIconComponent } from '../../../../shared/components/fip-icon/fip-icon.component';

@Component({
  selector: 'app-flight-information',
  imports: [DatePipe, FipIconComponent],
  templateUrl: './flight-information.component.html',
  styleUrl: './flight-information.component.scss'
})
export class FlightInformationComponent {
  @Input({ required: true }) flight!: FlightDetail;

  formatPosition(latitude: number | null, longitude: number | null): string {
    return latitude === null || longitude === null
      ? '—'
      : `${latitude.toFixed(4)}°, ${longitude.toFixed(4)}°`;
  }

  get statusLabel(): string {
    return this.flight.endTime ? 'Completed' : 'In progress';
  }
}
