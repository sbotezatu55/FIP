import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FlightsApiService } from './flights-api.service';

describe('FlightsApiService detail methods', () => {
  let service: FlightsApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FlightsApiService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(FlightsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('requests a flight by ID', () => {
    service.getFlight('flight-1').subscribe();

    const request = httpTesting.expectOne('/api/flights/flight-1');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('requests the flight summary by ID', () => {
    service.getFlightSummary('flight-1').subscribe();

    const request = httpTesting.expectOne('/api/flights/flight-1/summary');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('requests flight telemetry by ID', () => {
    service.getFlightTelemetry('flight-1').subscribe();

    const request = httpTesting.expectOne('/api/flights/flight-1/telemetry');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('requests flight events by ID', () => {
    service.getFlightEvents('flight-1').subscribe();

    const request = httpTesting.expectOne('/api/flights/flight-1/events');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('uploads a flight file as FormData without setting the multipart header', () => {
    const file = new File(['{}'], 'trajectory.json', { type: 'application/json' });
    service.importFlight(file).subscribe();

    const request = httpTesting.expectOne('/api/flights/import');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect((request.request.body as FormData).get('file')).toBe(file);
    expect(request.request.headers.has('Content-Type')).toBe(false);
    request.flush({});
  });
});
