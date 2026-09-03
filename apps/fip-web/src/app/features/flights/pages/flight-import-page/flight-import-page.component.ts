import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse, HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Observable, timeout } from 'rxjs';
import { FlightImportCandidate, FlightImportPreviewResult, FlightImportResult } from '../../models/flight-import-result';
import { FlightsApiService } from '../../services/flights-api.service';
import { FipIconComponent } from '../../../../shared/components/fip-icon/fip-icon.component';

export type FlightImportState = 'idle' | 'uploading' | 'processing' | 'completed' | 'failed';

@Component({
  selector: 'app-flight-import-page',
  imports: [DatePipe, DecimalPipe, RouterLink, FipIconComponent],
  templateUrl: './flight-import-page.component.html',
  styleUrl: './flight-import-page.component.scss'
})
export class FlightImportPageComponent {
  private readonly flightsApi = inject(FlightsApiService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  selectedFile: File | null = null;
  importState: FlightImportState = 'idle';
  uploadPercent: number | null = null;
  importResult: FlightImportResult | null = null;
  previewResult: FlightImportPreviewResult | null = null;
  busyCandidateId: string | null = null;
  errorMessage: string | null = null;
  debugMessage: string | null = null;
  candidateActionMessage: string | null = null;

  get isImporting(): boolean {
    return this.importState === 'uploading' || this.importState === 'processing';
  }

  get displayedCandidates(): FlightImportCandidate[] {
    return this.previewResult?.candidates.slice(0, 100) ?? [];
  }

  onFileSelected(event: Event): void {
    if (this.isImporting) return;

    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.importState = 'idle';
    this.uploadPercent = null;
    this.importResult = null;
    this.previewResult = null;
    this.errorMessage = null;
    this.setDebugMessage(this.selectedFile
      ? `Selected ${this.selectedFile.name} (${this.selectedFile.size} bytes).`
      : 'No file selected.');
  }

  importFlight(): void {
    if (!this.selectedFile || this.isImporting) return;

    this.importState = 'uploading';
    this.uploadPercent = null;
    this.importResult = null;
    this.errorMessage = null;
    this.setDebugMessage(`Starting ${this.selectedFile.name.toLowerCase().endsWith('.parquet') ? 'Parquet preview' : 'JSON import'} request.`);

    if (this.selectedFile.name.toLowerCase().endsWith('.parquet')) {
      this.flightsApi.previewImport(this.selectedFile).pipe(timeout({ each: 60000 })).subscribe({
        next: (response) => {
          this.setDebugMessage(`Preview response received: HTTP ${response.status}.`);
          if (!response.body) {
            this.importState = 'failed';
            this.errorMessage = 'The preview service returned an empty response.';
            this.setDebugMessage('Preview response contained no body.');
            return;
          }

          this.previewResult = response.body;
          this.importState = 'completed';
          this.setDebugMessage(`Preview completed: ${response.body.candidates.length} candidates returned.`);
        },
        error: (error: HttpErrorResponse) => {
          this.importState = 'failed';
          this.errorMessage = this.getErrorMessage(error);
          this.setDebugMessage(`Preview request failed: ${error.status || 'network error'}.`);
        }
      });
      return;
    }

    const request = this.flightsApi.importFlight(this.selectedFile) as Observable<HttpEvent<FlightImportResult>>;
    request.pipe(timeout({ each: 60000 })).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.Sent) {
          this.importState = 'processing';
          this.setDebugMessage('Request sent; waiting for the server response.');
          return;
        }

        if (event.type === HttpEventType.UploadProgress) {
          const total = event.total;
          this.importState = 'uploading';
          this.uploadPercent = total ? Math.round((100 * event.loaded) / total) : null;

          if (this.uploadPercent === 100) {
            this.importState = 'processing';
          }
          return;
        }

        if (event.type === HttpEventType.Response) {
          const response = event as HttpResponse<FlightImportResult>;
          this.importResult = response.body as FlightImportResult;
          if (!response.body) {
            this.importState = 'failed';
            this.errorMessage = 'The import service returned an empty response.';
            return;
          }

          this.importState = 'completed';
          this.errorMessage = null;

          if (!this.isValidFlightId(this.importResult.flightId)) {
            this.importState = 'failed';
            this.errorMessage = 'The import succeeded but did not return a valid flight ID.';
            return;
          }

          void this.router.navigate(['/flights', this.importResult.flightId]);
        }
      },
      error: (error: HttpErrorResponse) => {
        this.importState = 'failed';
        this.errorMessage = this.getErrorMessage(error);
        this.setDebugMessage(`Import request failed: ${error.status || 'network error'}.`);
      }
    });
  }

  importCandidate(candidate: FlightImportCandidate): void {
    if (!this.previewResult || this.busyCandidateId) return;
    this.busyCandidateId = candidate.candidateId;
    this.candidateActionMessage = `Importing ${candidate.callsign || candidate.icao24}...`;
    this.setDebugMessage(`Candidate import clicked: ${candidate.candidateId}.`);
    this.flightsApi.importPreviewCandidate(this.previewResult.previewId, candidate.candidateId).subscribe({
      next: result => {
        this.previewResult!.candidates = this.previewResult!.candidates.filter(item => item.candidateId !== candidate.candidateId);
        this.busyCandidateId = null;
        this.importResult = result;
        this.candidateActionMessage = `${candidate.callsign || candidate.icao24} imported successfully.`;
        this.setDebugMessage(`Candidate import completed: HTTP 200.`);
        if (this.isValidFlightId(result.flightId)) {
          void this.router.navigate(['/flights', result.flightId]);
        } else {
          this.errorMessage = 'The import succeeded but did not return a valid flight ID.';
        }
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(error);
        this.candidateActionMessage = `Candidate import failed: ${this.errorMessage}`;
        this.busyCandidateId = null;
        this.setDebugMessage(`Candidate import failed: ${error.status || 'network error'}.`);
      }
    });
  }

  ignoreCandidate(candidate: FlightImportCandidate): void {
    if (!this.previewResult || this.busyCandidateId) return;
    this.busyCandidateId = candidate.candidateId;
    this.candidateActionMessage = `Ignoring ${candidate.callsign || candidate.icao24}...`;
    this.setDebugMessage(`Candidate ignore clicked: ${candidate.candidateId}.`);
    this.flightsApi.ignorePreviewCandidate(this.previewResult.previewId, candidate.candidateId).subscribe({
      next: () => {
        this.previewResult!.candidates = this.previewResult!.candidates.filter(item => item.candidateId !== candidate.candidateId);
        this.busyCandidateId = null;
        this.candidateActionMessage = `${candidate.callsign || candidate.icao24} ignored.`;
        this.setDebugMessage(`Candidate ignore completed: HTTP 200.`);
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(error);
        this.candidateActionMessage = `Candidate ignore failed: ${this.errorMessage}`;
        this.busyCandidateId = null;
        this.setDebugMessage(`Candidate ignore failed: ${error.status || 'network error'}.`);
      }
    });
  }

  reviewCandidate(candidate: FlightImportCandidate): void {
    this.errorMessage = `${candidate.callsign || candidate.icao24} requires review before import.`;
    this.candidateActionMessage = this.errorMessage;
    this.setDebugMessage(`Review requested for candidate ${candidate.candidateId}.`);
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    const payload = error.error;
    if (payload && typeof payload === 'object' && 'message' in payload && typeof payload.message === 'string') {
      return payload.message;
    }

    if (payload && typeof payload === 'object' && 'title' in payload && typeof payload.title === 'string') {
      return payload.title;
    }

    if (typeof payload === 'string' && payload.trim()) return payload;
    return 'Unable to import flight data. Please check the file and try again.';
  }

  private isValidFlightId(value: string | null | undefined): boolean {
    return !!value && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }

  private setDebugMessage(message: string): void {
    this.debugMessage = message;
    console.debug(`[FlightImport] ${message}`);
    this.changeDetector.markForCheck();
  }
}
