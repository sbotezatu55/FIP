import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FlightDetail } from '../../models/flight-detail';

@Component({
  selector: 'app-flight-information',
  imports: [DatePipe],
  templateUrl: './flight-information.component.html',
  styleUrl: './flight-information.component.scss'
})
export class FlightInformationComponent {
  @Input({ required: true }) flight!: FlightDetail;

  formatPosition(latitude: number | null, longitude: number | null): string {
    return latitude === null || longitude === null
      ? '—'
      : `${latitude}, ${longitude}`;
  }
}
