import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FlightEvent } from '../../models/flight-event';
import { flightEventClass, flightEventLabel } from '../../services/flight-event-labels';

@Component({
  selector: 'app-flight-event-timeline',
  imports: [DatePipe],
  templateUrl: './flight-event-timeline.component.html',
  styleUrl: './flight-event-timeline.component.scss'
})
export class FlightEventTimelineComponent {
  @Input() events: readonly FlightEvent[] = [];

  get chronologicalEvents(): readonly FlightEvent[] {
    return [...this.events].sort((left, right) => {
      const timeDifference = Date.parse(left.timestamp) - Date.parse(right.timestamp);
      return Number.isNaN(timeDifference) ? 0 : timeDifference;
    });
  }

  eventLabel(type: string): string {
    return flightEventLabel(type);
  }

  eventClass(type: string): string {
    return flightEventClass(type);
  }
}
