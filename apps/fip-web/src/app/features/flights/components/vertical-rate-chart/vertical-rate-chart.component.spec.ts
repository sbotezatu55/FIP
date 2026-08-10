import { TestBed } from '@angular/core/testing';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { toVerticalRateChartPoints } from '../altitude-chart/telemetry-chart-utils';
import { VerticalRateChartComponent } from './vertical-rate-chart.component';

const point = (timestamp: string, verticalRateFeetPerMinute: number | null): FlightTelemetryPoint => ({
  timestamp,
  latitude: 52,
  longitude: 4,
  altitudeFeet: null,
  groundSpeedKnots: null,
  trackDegrees: null,
  verticalRateFeetPerMinute
});

describe('VerticalRateChartComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerticalRateChartComponent]
    }).compileComponents();
  });

  it('preserves positive, negative, and zero vertical rates chronologically', () => {
    expect(toVerticalRateChartPoints([
      point('2020-01-01T00:00:02Z', -1800),
      point('2020-01-01T00:00:00Z', 2200),
      point('2020-01-01T00:00:01Z', 0),
      point('2020-01-01T00:00:03Z', null),
      point('not-a-date', 900)
    ])).toEqual([
      { x: 0, y: 2200, timestamp: '2020-01-01T00:00:00Z' },
      { x: 1, y: 0, timestamp: '2020-01-01T00:00:01Z' },
      { x: 2, y: -1800, timestamp: '2020-01-01T00:00:02Z' }
    ]);
  });

  it('renders an empty state when no vertical-rate samples are usable', () => {
    const fixture = TestBed.createComponent(VerticalRateChartComponent);
    fixture.componentInstance.telemetry = [point('2020-01-01T00:00:00Z', null)];
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Vertical-rate data is unavailable for this flight.');
  });
});
