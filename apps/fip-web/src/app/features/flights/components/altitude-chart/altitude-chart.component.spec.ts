import { TestBed } from '@angular/core/testing';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { AltitudeChartComponent } from './altitude-chart.component';
import { toAltitudeChartPoints } from './telemetry-chart-utils';

const point = (timestamp: string, altitudeFeet: number | null): FlightTelemetryPoint => ({
  timestamp,
  latitude: 52,
  longitude: 4,
  altitudeFeet,
  groundSpeedKnots: null,
  trackDegrees: null,
  verticalRateFeetPerMinute: null
});

describe('AltitudeChartComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AltitudeChartComponent]
    }).compileComponents();
  });

  it('maps usable altitude telemetry chronologically to elapsed seconds', () => {
    expect(toAltitudeChartPoints([
      point('2020-01-01T00:00:02Z', 2000),
      point('2020-01-01T00:00:00Z', 1000),
      point('2020-01-01T00:00:01Z', null),
      point('not-a-date', 3000)
    ])).toEqual([
      { x: 0, y: 1000, timestamp: '2020-01-01T00:00:00Z' },
      { x: 2, y: 2000, timestamp: '2020-01-01T00:00:02Z' }
    ]);
  });

  it('renders an empty state when no altitude samples are usable', () => {
    const fixture = TestBed.createComponent(AltitudeChartComponent);
    fixture.componentInstance.telemetry = [point('2020-01-01T00:00:00Z', null)];
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Altitude data is unavailable for this flight.');
  });
});
