import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import Chart from 'chart.js/auto';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import {
  formatElapsedSeconds,
  formatTelemetryTimestamp,
  toGroundspeedChartPoints
} from '../altitude-chart/telemetry-chart-utils';

@Component({
  selector: 'app-groundspeed-chart',
  templateUrl: './groundspeed-chart.component.html',
  styleUrl: './groundspeed-chart.component.scss'
})
export class GroundspeedChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() telemetry: readonly FlightTelemetryPoint[] = [];
  @ViewChild('chartCanvas') private chartCanvas?: ElementRef<HTMLCanvasElement>;

  private chart?: Chart<'line'>;

  get chartPoints() {
    return toGroundspeedChartPoints(this.telemetry);
  }

  ngAfterViewInit(): void {
    this.renderChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['telemetry']) {
      if (this.chart) {
        this.updateChart();
      } else if (this.chartCanvas) {
        this.renderChart();
      }
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = undefined;
  }

  private renderChart(): void {
    const points = this.chartPoints;
    if (!this.chartCanvas || points.length === 0) return;

    const startTimestamp = points[0].timestamp;
    this.chart = new Chart(this.chartCanvas.nativeElement, {
      type: 'line',
      data: {
        datasets: [{
          label: 'Groundspeed',
          data: points,
          parsing: false,
          borderColor: '#21d4df',
          backgroundColor: 'rgb(33 212 223 / 10%)',
          borderWidth: 2,
          fill: true,
          pointRadius: 0,
          pointHoverRadius: 4,
          pointHoverBackgroundColor: '#85e63f',
          tension: 0,
          spanGaps: false
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        interaction: {
          mode: 'nearest',
          intersect: false
        },
        plugins: {
          legend: { display: false },
          decimation: {
            enabled: true,
            algorithm: 'lttb',
            samples: 1000
          },
          tooltip: {
            callbacks: {
              title: (items) => {
                const point = items[0]?.raw as { timestamp?: string } | undefined;
                return point?.timestamp ? formatTelemetryTimestamp(point.timestamp) : '';
              },
              label: (context) => `Groundspeed: ${Number(context.parsed.y).toLocaleString('en-US')} kt`
            }
          }
        },
        scales: {
          x: {
            type: 'linear',
            title: { display: true, text: 'Time', color: '#a2afb6' },
            ticks: {
              color: '#a2afb6',
              maxTicksLimit: 8,
              callback: (value) => formatElapsedSeconds(Number(value), startTimestamp)
            },
            grid: { color: 'rgb(71 152 183 / 18%)' }
          },
          y: {
            beginAtZero: true,
            title: { display: true, text: 'Groundspeed (kt)', color: '#a2afb6' },
            ticks: {
              color: '#a2afb6',
              callback: (value) => Number(value).toLocaleString('en-US')
            },
            grid: { color: 'rgb(71 152 183 / 18%)' }
          }
        }
      }
    });
  }

  private updateChart(): void {
    this.chart?.destroy();
    this.chart = undefined;
    this.renderChart();
  }
}
