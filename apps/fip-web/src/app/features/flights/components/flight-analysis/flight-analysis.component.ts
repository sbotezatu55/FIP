import { DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FlightEvent } from '../../models/flight-event';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { FipIconComponent } from '../../../../shared/components/fip-icon/fip-icon.component';

@Component({
  selector: 'app-flight-analysis',
  imports: [DecimalPipe, FipIconComponent],
  templateUrl: './flight-analysis.component.html',
  styleUrl: './flight-analysis.component.scss'
})
export class FlightAnalysisComponent {
  @Input() telemetry: readonly FlightTelemetryPoint[] = [];
  @Input() events: readonly FlightEvent[] = [];
}
