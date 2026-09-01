import { Component, Input } from '@angular/core';

export type FipIconName = 'ingest' | 'normalize' | 'reconstruct' | 'detect' | 'analyze' | 'visualize' | 'aircraft' | 'airport' | 'runway' | 'trajectory' | 'flight-event' | 'telemetry' | 'clock' | 'distance' | 'altitude' | 'speed';

@Component({
  selector: 'app-fip-icon',
  standalone: true,
  template: `
    <svg class="fip-icon" [attr.width]="size" [attr.height]="size" viewBox="0 0 24 24" [attr.role]="label ? 'img' : null" [attr.aria-label]="label" [attr.aria-hidden]="label ? null : 'true'" focusable="false">
      <use [attr.href]="iconUrl" />
    </svg>
  `,
  styles: `:host { display: inline-flex; line-height: 0; color: var(--fip-cyan); } .fip-icon { display: block; overflow: visible; color: inherit; }`
})
export class FipIconComponent {
  @Input() name: FipIconName = 'telemetry';
  @Input() size = 20;
  @Input() label: string | null = null;

  get iconUrl(): string {
    return `/icons/fip/${this.name}.svg#fip-icon`;
  }
}
