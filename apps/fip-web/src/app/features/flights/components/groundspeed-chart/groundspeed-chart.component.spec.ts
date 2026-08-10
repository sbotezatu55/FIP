import { TestBed } from '@angular/core/testing';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { GroundspeedChartComponent } from './groundspeed-chart.component';
import { toGroundspeedChartPoints } from '../altitude-chart/telemetry-chart-utils';

const point = (timestamp: string, groundSpeedKnots: number | null): FlightTelemetryPoint => ({
  timestamp,
  latitude: 52,
  longitude: 4,
  altitudeFeet: null,
  groundSpeedKnots,
  trackDegrees: null,
  verticalRateFeetPerMinute: null
});

describe('GroundspeedChartComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroundspeedChartComponent]
    }).compileComponents();
  });

  it('maps usable groundspeed telemetry chronologically to elapsed seconds', () => {
    expect(toGroundspeedChartPoints([
      point('2020-01-01T00:00:02Z', 220),
      point('2020-01-01T00:00:00Z', 140),
      point('2020-01-01T00:00:01Z', null),
      point('not-a-date', 300)
    ])).toEqual([
      { x: 0, y: 140, timestamp: '2020-01-01T00:00:00Z' },
      { x: 2, y: 220, timestamp: '2020-01-01T00:00:02Z' }
    ]);
  });

  it('renders an empty state when no groundspeed samples are usable', () => {
    const fixture = TestBed.createComponent(GroundspeedChartComponent);
    fixture.componentInstance.telemetry = [point('2020-01-01T00:00:00Z', null)];
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Groundspeed data is unavailable for this flight.');
  });
});
