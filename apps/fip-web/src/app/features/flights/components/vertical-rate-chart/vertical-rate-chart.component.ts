import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import Chart from 'chart.js/auto';
import { FlightTelemetryPoint } from '../../models/flight-telemetry-point';
import {
  formatElapsedSeconds,
  formatTelemetryTimestamp,
  toVerticalRateChartPoints
} from '../altitude-chart/telemetry-chart-utils';

@Component({
  selector: 'app-vertical-rate-chart',
  templateUrl: './vertical-rate-chart.component.html',
  styleUrl: './vertical-rate-chart.component.scss'
})
export class VerticalRateChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() telemetry: readonly FlightTelemetryPoint[] = [];
  @ViewChild('chartCanvas') private chartCanvas?: ElementRef<HTMLCanvasElement>;

  private chart?: Chart<'line'>;

  get chartPoints() {
    return toVerticalRateChartPoints(this.telemetry);
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
    const cyan = this.getThemeColor('--fip-cyan', '#21d4df');
    const axis = this.getThemeColor('--fip-axis', '#f1f5f7');
    this.chart = new Chart(this.chartCanvas.nativeElement, {
      type: 'line',
      data: {
        datasets: [{
          label: 'Vertical Rate',
          data: points,
          parsing: false,
          borderColor: cyan,
          backgroundColor: 'rgb(33 212 223 / 9%)',
          borderWidth: 2,
          fill: true,
          pointRadius: 0,
          pointHoverRadius: 4,
          pointHoverBackgroundColor: '#21d4df',
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
              label: (context) => {
                const value = Number(context.parsed.y);
                const signedValue = value > 0 ? `+${value.toLocaleString('en-US')}` : value.toLocaleString('en-US');
                const phase = value > 0 ? ' — Climb' : value < 0 ? ' — Descent' : ' — Level';
                return `Vertical Rate: ${signedValue} ft/min${phase}`;
              }
            }
          }
        },
        scales: {
          x: {
            type: 'linear',
            title: { display: true, text: 'Time', color: axis },
            ticks: {
              color: axis,
              maxTicksLimit: 8,
              callback: (value) => formatElapsedSeconds(Number(value), startTimestamp)
            },
            grid: { color: 'rgb(71 152 183 / 18%)' }
          },
          y: {
            beginAtZero: true,
            title: { display: true, text: 'Vertical Rate (ft/min)', color: '#a2afb6' },
            grace: '5%',
            ticks: {
              color: '#a2afb6',
              callback: (value) => Number(value).toLocaleString('en-US')
            },
            grid: {
              color: 'rgb(71 152 183 / 18%)',
              lineWidth: 1
            }
          }
        }
      }
    });
  }

  private getThemeColor(variable: string, fallback: string): string {
    return getComputedStyle(document.documentElement).getPropertyValue(variable).trim() || fallback;
  }

  private updateChart(): void {
    this.chart?.destroy();
    this.chart = undefined;
    this.renderChart();
  }
}
