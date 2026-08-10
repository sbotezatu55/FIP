import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { FlightDetail } from '../../models/flight-detail';
import { FlightSummary } from '../../models/flight-summary';
import { FlightsApiService } from '../../services/flights-api.service';
import { FlightDetailPageComponent } from './flight-detail-page.component';

const flightId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const detail: FlightDetail = {
  id: flightId,
  icao24: '484506',
  callsign: 'TRA051',
  startTime: '2018-05-30T12:04:00Z',
  endTime: '2018-05-30T14:37:00Z',
  duration: '02:33:00',
  departureLatitude: 52.32397,
  departureLongitude: 4.73942,
  arrivalLatitude: 40.6413,
  arrivalLongitude: -73.7781,
  maximumAltitudeFeet: 37000
};

const summary: FlightSummary = {
  flightId,
  callsign: 'TRA051',
  icao24: '484506',
  startTime: detail.startTime,
  endTime: detail.endTime,
  duration: '02:33:00',
  maximumAltitudeFeet: 37000,
  maximumGroundSpeedKnots: 482,
  averageGroundSpeedKnots: 421,
  maximumVerticalRateFeetPerMinute: 2240,
  minimumVerticalRateFeetPerMinute: -1800,
  distanceTraveledNauticalMiles: 1124,
  takeoffTime: null,
  landingTime: null,
  flightTime: null
};

describe('FlightDetailPageComponent', () => {
  let flightsApi: {
    getFlight: ReturnType<typeof vi.fn>;
    getFlightSummary: ReturnType<typeof vi.fn>;
    getFlightTelemetry: ReturnType<typeof vi.fn>;
    getFlightEvents: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    flightsApi = {
      getFlight: vi.fn().mockReturnValue(of(detail)),
      getFlightSummary: vi.fn().mockReturnValue(of(summary)),
      getFlightTelemetry: vi.fn().mockReturnValue(of([])),
      getFlightEvents: vi.fn().mockReturnValue(of([]))
    };

    await TestBed.configureTestingModule({
      imports: [FlightDetailPageComponent],
      providers: [
        provideRouter([{ path: 'flights/:id', component: FlightDetailPageComponent }]),
        { provide: FlightsApiService, useValue: flightsApi }
      ]
    }).compileComponents();
  });

  async function navigateToFlight() {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(`/flights/${flightId}`, FlightDetailPageComponent);
    harness.detectChanges();
    return harness;
  }

  it('loads the route ID and renders flight information and summary cards', async () => {
    const harness = await navigateToFlight();
    const element = harness.routeNativeElement as HTMLElement;

    expect(flightsApi.getFlight).toHaveBeenCalledWith(flightId);
    expect(flightsApi.getFlightSummary).toHaveBeenCalledWith(flightId);
    expect(flightsApi.getFlightTelemetry).toHaveBeenCalledWith(flightId);
    expect(flightsApi.getFlightEvents).toHaveBeenCalledWith(flightId);
    expect(element.textContent).toContain('TRA051');
    expect(element.textContent).toContain('ICAO24 484506');
    expect(element.textContent).toContain('2h 33m');
    expect(element.textContent).toContain('37,000 ft');
    expect(element.textContent).toContain('1,124 nm');
    expect(element.textContent).toContain('482 kt');
  });

  it('shows a loading state while requests are pending', async () => {
    flightsApi.getFlight.mockReturnValue(new Subject<FlightDetail>());
    const harness = await navigateToFlight();

    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('Loading flight...');
  });

  it('shows not found for a missing flight', async () => {
    flightsApi.getFlight.mockReturnValue(throwError(() => ({ status: 404 })));
    const harness = await navigateToFlight();

    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('Flight not found.');
  });

  it('preserves detail data when summary loading fails', async () => {
    flightsApi.getFlightSummary.mockReturnValue(throwError(() => new Error('summary failure')));
    const harness = await navigateToFlight();

    const element = harness.routeNativeElement as HTMLElement;
    expect(element.textContent).toContain('TRA051');
    expect(element.textContent).toContain('Flight summary is unavailable.');
  });

  it('shows a trajectory-specific error when telemetry loading fails', async () => {
    flightsApi.getFlightTelemetry.mockReturnValue(throwError(() => new Error('telemetry failure')));
    const harness = await navigateToFlight();

    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('Unable to load flight trajectory.');
  });

  it('provides a back link to the Flights page', async () => {
    const harness = await navigateToFlight();

    expect((harness.routeNativeElement?.querySelector('.back-link') as HTMLAnchorElement).getAttribute('href'))
      .toBe('/flights');
  });
});
