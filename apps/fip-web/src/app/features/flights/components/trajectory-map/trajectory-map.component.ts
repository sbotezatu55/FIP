import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import * as L from 'leaflet';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import { trajectoryMapConfig } from './map-config';

export type TrajectoryCoordinate = [number, number];

export function toTrajectoryCoordinates(points: readonly FlightTelemetryPoint[]): TrajectoryCoordinate[] {
  return points
    .filter((point) =>
      point.latitude !== null &&
      point.longitude !== null &&
      point.latitude >= -90 &&
      point.latitude <= 90 &&
      point.longitude >= -180 &&
      point.longitude <= 180)
    .map((point) => [point.latitude as number, point.longitude as number]);
}

@Component({
  selector: 'app-trajectory-map',
  templateUrl: './trajectory-map.component.html',
  styleUrl: './trajectory-map.component.scss'
})
export class TrajectoryMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() telemetry: readonly FlightTelemetryPoint[] = [];
  @ViewChild('mapContainer') private mapContainer?: ElementRef<HTMLDivElement>;

  private map?: L.Map;
  private routeLayer?: L.LayerGroup;

  get validCoordinates(): TrajectoryCoordinate[] {
    return toTrajectoryCoordinates(this.telemetry);
  }

  ngAfterViewInit(): void {
    this.renderMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['telemetry']) {
      if (this.map) {
        this.renderRoute();
      } else if (this.mapContainer) {
        this.renderMap();
      }
    }
  }

  ngOnDestroy(): void {
    this.map?.remove();
    this.map = undefined;
  }

  private renderMap(): void {
    const coordinates = this.validCoordinates;
    if (!this.mapContainer || coordinates.length === 0) return;

    this.map = L.map(this.mapContainer.nativeElement, {
      center: trajectoryMapConfig.initialCenter,
      zoom: trajectoryMapConfig.initialZoom,
      zoomControl: true
    });

    L.tileLayer(trajectoryMapConfig.tileUrl, {
      attribution: trajectoryMapConfig.attribution,
      maxZoom: 19
    }).addTo(this.map);

    this.renderRoute();
  }

  private renderRoute(): void {
    if (!this.map) return;

    this.routeLayer?.clearLayers();
    this.routeLayer = L.layerGroup().addTo(this.map);

    const coordinates = this.validCoordinates;
    if (coordinates.length === 0) return;

    L.polyline(coordinates, {
      color: '#1b6ca8',
      weight: 4,
      opacity: 0.9,
      lineJoin: 'round'
    }).addTo(this.routeLayer);

    const first = coordinates[0];
    const last = coordinates[coordinates.length - 1];
    this.createMarker(first, 'Trajectory Start', 'trajectory-marker trajectory-marker--start').addTo(this.routeLayer);
    if (coordinates.length > 1) {
      this.createMarker(last, 'Trajectory End', 'trajectory-marker trajectory-marker--end').addTo(this.routeLayer);
    }

    if (coordinates.length === 1) {
      this.map.setView(first, trajectoryMapConfig.singlePointZoom);
    } else {
      this.map.fitBounds(L.latLngBounds(coordinates), { padding: trajectoryMapConfig.fitBoundsPadding });
    }
  }

  private createMarker(position: TrajectoryCoordinate, label: string, className: string): L.Marker {
    return L.marker(position, {
      icon: L.divIcon({
        className: '',
        html: `<span class="${className}"></span>`,
        iconSize: [16, 16],
        iconAnchor: [8, 8]
      })
    }).bindPopup(label);
  }
}
