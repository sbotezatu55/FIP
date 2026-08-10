import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { FlightListItem } from '../../models/flight-list-item';
import { FlightsApiService } from '../../services/flights-api.service';
import { FlightListPageComponent } from './flight-list-page.component';

const flight: FlightListItem = {
  id: 'flight-1',
  icao24: '484506',
  callsign: 'TRA051',
  startTime: '2018-05-30T12:04:00Z',
  endTime: '2018-05-30T14:37:00Z',
  duration: '02:33:00',
  maximumAltitudeFeet: 37000,
  departureLatitude: null,
  departureLongitude: null,
  arrivalLatitude: null,
  arrivalLongitude: null,
  telemetryPointCount: 5384,
  eventCount: 5
};

describe('FlightListPageComponent', () => {
  let flightsApi: { getFlights: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    flightsApi = { getFlights: vi.fn().mockReturnValue(of([])) };

    await TestBed.configureTestingModule({
      imports: [FlightListPageComponent],
      providers: [
        provideRouter([{ path: 'flights/:id', component: FlightListPageComponent }]),
        { provide: FlightsApiService, useValue: flightsApi }
      ]
    }).compileComponents();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(FlightListPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('renders returned flights and links to the flight detail route', () => {
    flightsApi.getFlights.mockReturnValue(of([flight]));
    const fixture = createComponent();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('TRA051');
    expect(element.textContent).toContain('484506');
    expect(element.textContent).toContain('2h 33m');
    expect(element.textContent).toContain('37,000 ft');
    expect((element.querySelector('.flight-callsign a') as HTMLAnchorElement).getAttribute('href'))
      .toBe('/flights/flight-1');
  });

  it('shows a loading state until the request completes', () => {
    const response$ = new Subject<FlightListItem[]>();
    flightsApi.getFlights.mockReturnValue(response$);
    const fixture = createComponent();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading flights...');
  });

  it('shows an empty state for an empty response', () => {
    const fixture = createComponent();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No flights have been imported yet.');
  });

  it('shows a friendly error state when the API fails', () => {
    flightsApi.getFlights.mockReturnValue(throwError(() => new Error('failure')));
    const fixture = createComponent();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Unable to load flights.');
  });

  it('navigates when a flight link is selected', () => {
    flightsApi.getFlights.mockReturnValue(of([flight]));
    const fixture = createComponent();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    (fixture.nativeElement.querySelector('.flight-callsign a') as HTMLAnchorElement).click();

    expect(navigateSpy).toHaveBeenCalled();
  });
});
