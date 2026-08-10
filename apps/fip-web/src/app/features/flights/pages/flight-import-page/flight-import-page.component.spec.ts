import { provideRouter, Router } from '@angular/router';
import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { FlightImportResult } from '../../models/flight-import-result';
import { FlightsApiService } from '../../services/flights-api.service';
import { FlightImportPageComponent } from './flight-import-page.component';

const result: FlightImportResult = {
  status: 'Imported',
  flightId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
  callsign: 'TRA051',
  icao24: '484506',
  pointsImported: 5384,
  startTime: '2018-05-30T12:04:00Z',
  endTime: '2018-05-30T14:37:00Z',
  eventsDetected: 5,
  warnings: []
};

describe('FlightImportPageComponent', () => {
  let flightsApi: { importFlight: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    flightsApi = { importFlight: vi.fn().mockReturnValue(of(new HttpResponse({ status: 201, body: result }))) };

    await TestBed.configureTestingModule({
      imports: [FlightImportPageComponent],
      providers: [provideRouter([]), { provide: FlightsApiService, useValue: flightsApi }]
    }).compileComponents();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(FlightImportPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  function selectFile(fixture: ReturnType<typeof createComponent>, file = new File(['{}'], 'trajectory.json', { type: 'application/json' })) {
    const input = fixture.nativeElement.querySelector('#flight-file') as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: [file] });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    return file;
  }

  it('disables import until a file is selected', () => {
    const fixture = createComponent();

    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBe(true);
  });

  it('stores and displays the selected file', () => {
    const fixture = createComponent();
    const file = selectFile(fixture);

    expect(fixture.componentInstance.selectedFile).toBe(file);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('trajectory.json');
  });

  it('calls the import service and displays the result', () => {
    const fixture = createComponent();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    selectFile(fixture);
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(flightsApi.importFlight).toHaveBeenCalledWith(fixture.componentInstance.selectedFile);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Flight imported successfully');
    expect(navigateSpy).toHaveBeenCalledWith(['/flights', result.flightId]);
  });

  it('shows upload progress and switches to truthful processing status at 100 percent', () => {
    const response$ = new Subject<HttpEvent<FlightImportResult>>();
    flightsApi.importFlight.mockReturnValue(response$);
    const fixture = createComponent();
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    selectFile(fixture);
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();

    response$.next({ type: HttpEventType.UploadProgress, loaded: 64, total: 100 });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Uploading: 64%');

    response$.next({ type: HttpEventType.UploadProgress, loaded: 100, total: 100 });
    expect(fixture.componentInstance.importState).toBe('processing');
    response$.next(new HttpResponse({ status: 201, body: result }));
    response$.complete();
  });

  it('disables the button while import is running', () => {
    const response$ = new Subject<HttpEvent<FlightImportResult>>();
    flightsApi.importFlight.mockReturnValue(response$);
    const fixture = createComponent();
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    selectFile(fixture);
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(button.disabled).toBe(true);
    expect(button.textContent).toContain('Importing...');
    response$.next(new HttpResponse({ status: 201, body: result }));
    response$.complete();
  });

  it('shows a friendly error when import fails', () => {
    flightsApi.importFlight.mockReturnValue(throwError(() => new Error('failure')));
    const fixture = createComponent();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');
    selectFile(fixture);
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Unable to import flight data.');
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('does not navigate before the final successful response', () => {
    const response$ = new Subject<HttpEvent<FlightImportResult>>();
    flightsApi.importFlight.mockReturnValue(response$);
    const fixture = createComponent();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');
    selectFile(fixture);
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();

    response$.next({ type: HttpEventType.UploadProgress, loaded: 100, total: 100 });
    expect(navigateSpy).not.toHaveBeenCalled();
    response$.next(new HttpResponse({ status: 201, body: result }));

    expect(navigateSpy).toHaveBeenCalledWith(['/flights', result.flightId]);
    response$.complete();
  });

  it('stays on the import page when the successful response has no flight ID', () => {
    const missingIdResult = { ...result, flightId: '' };
    flightsApi.importFlight.mockReturnValue(of(new HttpResponse({ status: 201, body: missingIdResult })));
    const fixture = createComponent();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');
    selectFile(fixture);
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(navigateSpy).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('did not return a valid flight ID');
  });
});
