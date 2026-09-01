import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-flight-profile-section',
  templateUrl: './flight-profile-section.component.html',
  styleUrl: './flight-profile-section.component.scss'
})
export class FlightProfileSectionComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) subtitle!: string;
  @Input({ required: true }) loading = false;
  @Input({ required: true }) errorMessage: string | null = null;
}
