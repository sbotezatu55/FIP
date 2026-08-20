import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FipIconComponent } from '../../../../shared/components/fip-icon/fip-icon.component';
import { FlightPipelineComponent } from '../../components/flight-pipeline/flight-pipeline.component';
import { RecentFlightsComponent } from '../../components/recent-flights/recent-flights.component';

@Component({
  selector: 'app-index-page',
  imports: [RouterLink, FipIconComponent, RecentFlightsComponent, FlightPipelineComponent],
  templateUrl: './index-page.component.html',
  styleUrl: './index-page.component.scss'
})
export class IndexPageComponent {}
