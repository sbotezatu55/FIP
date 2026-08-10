import { TestBed } from '@angular/core/testing';
import { FlightEvent } from '../../models/flight-event';
import { FlightEventTimelineComponent } from './flight-event-timeline.component';

const event = (type: string, timestamp: string, description: string | null = null): FlightEvent => ({
  id: `${type}-${timestamp}`,
  flightId: 'flight-1',
  type,
  timestamp,
  latitude: null,
  longitude: null,
  altitudeFeet: null,
  description
});

describe('FlightEventTimelineComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FlightEventTimelineComponent]
    }).compileComponents();
  });

  it('orders events chronologically and maps event labels', () => {
    const fixture = TestBed.createComponent(FlightEventTimelineComponent);
    fixture.componentInstance.events = [
      event('Landing', '2020-01-01T02:00:00Z'),
      event('Takeoff', '2020-01-01T00:00:00Z'),
      event('TopOfClimb', '2020-01-01T00:30:00Z'),
      event('TopOfDescent', '2020-01-01T01:30:00Z'),
      event('TelemetryGap', '2020-01-01T01:00:00Z', '4m 19s without telemetry')
    ];
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const headings = Array.from(element.querySelectorAll('h3')).map((heading) => heading.textContent?.trim());
    expect(headings).toEqual(['Takeoff', 'Top of Climb', 'Telemetry Gap', 'Top of Descent', 'Landing']);
    expect(element.textContent).toContain('4m 19s without telemetry');
  });

  it('shows the empty state when no events are supplied', () => {
    const fixture = TestBed.createComponent(FlightEventTimelineComponent);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No flight events were detected.');
  });
});
