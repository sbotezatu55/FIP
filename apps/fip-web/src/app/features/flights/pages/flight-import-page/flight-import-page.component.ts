import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse, HttpEventType, HttpResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FlightImportResult } from '../../models/flight-import-result';
import { FlightsApiService } from '../../services/flights-api.service';

export type FlightImportState = 'idle' | 'uploading' | 'processing' | 'completed' | 'failed';

@Component({
  selector: 'app-flight-import-page',
  imports: [DecimalPipe, RouterLink],
  templateUrl: './flight-import-page.component.html',
  styleUrl: './flight-import-page.component.scss'
})
export class FlightImportPageComponent {
  private readonly flightsApi = inject(FlightsApiService);
  private readonly router = inject(Router);

  selectedFile: File | null = null;
  importState: FlightImportState = 'idle';
  uploadPercent: number | null = null;
  importResult: FlightImportResult | null = null;
  errorMessage: string | null = null;

  get isImporting(): boolean {
    return this.importState === 'uploading' || this.importState === 'processing';
  }

  onFileSelected(event: Event): void {
    if (this.isImporting) return;

    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.importState = 'idle';
    this.uploadPercent = null;
    this.importResult = null;
    this.errorMessage = null;
  }

  importFlight(): void {
    if (!this.selectedFile || this.isImporting) return;

    this.importState = 'uploading';
    this.uploadPercent = null;
    this.importResult = null;
    this.errorMessage = null;

    this.flightsApi.importFlight(this.selectedFile).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.Sent) {
          this.importState = 'processing';
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

        if (event instanceof HttpResponse) {
          this.importResult = event.body;
          if (!event.body) {
            this.importState = 'failed';
            this.errorMessage = 'The import service returned an empty response.';
            return;
          }

          this.importState = 'completed';
          this.errorMessage = null;

          if (!this.isValidFlightId(event.body.flightId)) {
            this.importState = 'failed';
            this.errorMessage = 'The import succeeded but did not return a valid flight ID.';
            return;
          }

          void this.router.navigate(['/flights', event.body.flightId]);
        }
      },
      error: (error: HttpErrorResponse) => {
        this.importState = 'failed';
        this.errorMessage = this.getErrorMessage(error);
      }
    });
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
}
