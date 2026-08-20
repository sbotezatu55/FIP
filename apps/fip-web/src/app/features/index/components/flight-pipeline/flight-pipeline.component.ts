import { Component } from '@angular/core';
import { FipIconComponent, FipIconName } from '../../../../shared/components/fip-icon/fip-icon.component';

interface PipelineStage {
  name: string;
  description: string;
  icon: FipIconName;
}

@Component({
  selector: 'app-flight-pipeline',
  imports: [FipIconComponent],
  templateUrl: './flight-pipeline.component.html',
  styleUrl: './flight-pipeline.component.scss'
})
export class FlightPipelineComponent {
  readonly stages: PipelineStage[] = [
    { name: 'Ingest', description: 'Collect raw flight data', icon: 'ingest' },
    { name: 'Normalize', description: 'Clean & standardize telemetry', icon: 'normalize' },
    { name: 'Reconstruct', description: 'Rebuild the flight trajectory', icon: 'reconstruct' },
    { name: 'Detect', description: 'Identify key flight events', icon: 'detect' },
    { name: 'Analyze', description: 'Generate metrics & insights', icon: 'analyze' },
    { name: 'Visualize', description: 'Interactive charts & maps', icon: 'visualize' }
  ];
}
