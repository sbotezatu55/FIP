import { TestBed } from '@angular/core/testing';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { toTrajectoryCoordinates, TrajectoryMapComponent } from './trajectory-map.component';

const point = (timestamp: string, latitude: number | null, longitude: number | null): FlightTelemetryPoint => ({
  timestamp,
  latitude,
  longitude,
  altitudeFeet: null,
  groundSpeedKnots: null,
  trackDegrees: null,
  verticalRateFeetPerMinute: null
});

describe('TrajectoryMapComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrajectoryMapComponent]
    }).compileComponents();
  });

  it('keeps valid coordinates in telemetry order', () => {
    expect(toTrajectoryCoordinates([
      point('2020-01-01T00:00:00Z', 52.1, 4.1),
      point('2020-01-01T00:00:01Z', 91, 4.2),
      point('2020-01-01T00:00:02Z', 52.3, 4.3),
      point('2020-01-01T00:00:03Z', null, 4.4)
    ])).toEqual([[52.1, 4.1], [52.3, 4.3]]);
  });

  it('accepts telemetry through its input and displays an empty state without valid points', () => {
    const fixture = TestBed.createComponent(TrajectoryMapComponent);
    fixture.componentInstance.telemetry = [point('2020-01-01T00:00:00Z', null, null)];
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Trajectory data is unavailable for this flight.');
  });
});
