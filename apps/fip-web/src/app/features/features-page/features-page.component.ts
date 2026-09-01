import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataPipelineComponent } from '../technology/components/data-pipeline/data-pipeline.component';
import { CapabilitySectionComponent } from '../technology/components/capability-section/capability-section.component';
import { DataSourcesComponent } from '../technology/components/data-sources/data-sources.component';
import { VisualizationSectionComponent } from '../technology/components/visualization-section/visualization-section.component';
import { RoadmapSectionComponent } from '../technology/components/roadmap-section/roadmap-section.component';

@Component({
  selector: 'app-features-page',
  imports: [RouterLink, DataPipelineComponent, CapabilitySectionComponent, DataSourcesComponent, VisualizationSectionComponent, RoadmapSectionComponent],
  templateUrl: './features-page.component.html',
  styleUrl: './features-page.component.scss'
})
export class FeaturesPageComponent {}
