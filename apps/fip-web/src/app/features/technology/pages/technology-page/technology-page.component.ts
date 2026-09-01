import { Component } from '@angular/core';
import { ArchitectureFlowComponent } from '../../components/architecture-flow/architecture-flow.component';
import { TechnologyGridComponent } from '../../components/technology-grid/technology-grid.component';
import { DataPipelineComponent } from '../../components/data-pipeline/data-pipeline.component';

@Component({
  selector: 'app-technology-page',
  imports: [ArchitectureFlowComponent, TechnologyGridComponent, DataPipelineComponent],
  templateUrl: './technology-page.component.html',
  styleUrl: './technology-page.component.scss'
})
export class TechnologyPageComponent {}
