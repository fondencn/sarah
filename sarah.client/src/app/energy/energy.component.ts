import { Component, OnDestroy, OnInit } from '@angular/core';
import { forkJoin, Subscription, timer } from 'rxjs';
import {
  buildStackedBars, EnergyDeviceStatistics, EnergyLiveDevice, EnergyStatisticsService,
  EnergySummary, EnergyTimeseriesPoint, StackedBar
} from '../services/energy-statistics.service';

export type EnergyRange = 'today' | '7d' | '30d' | 'year';
export type EnergySortKey = 'deviceName' | 'currentPowerW' | 'energyKwh';

const CHART_DEVICE_COUNT = 5;
const LIVE_REFRESH_MS = 15000;
export const CHART_COLORS = ['#0d6efd', '#198754', '#fd7e14', '#6f42c1', '#d63384', '#6c757d'];

@Component({
  selector: 'app-energy',
  templateUrl: './energy.component.html',
  styleUrls: ['./energy.component.css']
})
export class EnergyComponent implements OnInit, OnDestroy {
  readonly ranges: { key: EnergyRange; label: string }[] = [
    { key: 'today', label: 'energy.today' },
    { key: '7d', label: 'energy.days7' },
    { key: '30d', label: 'energy.days30' },
    { key: 'year', label: 'energy.year' }
  ];

  range: EnergyRange = 'today';
  isLoading = false;
  hasError = false;
  summary: EnergySummary | null = null;
  devices: EnergyDeviceStatistics[] = [];
  bars: StackedBar[] = [];
  chartDevices: EnergyDeviceStatistics[] = [];
  hidden = new Set<number>();
  liveIndicator = false;
  sortKey: EnergySortKey = 'energyKwh';
  sortDescending = true;
  selected: EnergyDeviceStatistics | null = null;
  selectedBars: StackedBar[] = [];

  private series = new Map<number, EnergyTimeseriesPoint[]>();
  private liveSubscription: Subscription | null = null;
  private loadGeneration = 0;
  private currentFrom = new Date();
  private currentTo = new Date();

  constructor(private statistics: EnergyStatisticsService) { }

  ngOnInit(): void {
    this.load();
    this.liveSubscription = timer(LIVE_REFRESH_MS, LIVE_REFRESH_MS).subscribe(() => this.refreshLive());
  }

  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
    this.liveSubscription = null;
  }

  setRange(range: EnergyRange): void {
    this.range = range;
    this.selected = null;
    this.load();
  }

  load(): void {
    const { from, to } = this.computeRange(this.range, new Date());
    const generation = ++this.loadGeneration;
    this.currentFrom = from;
    this.currentTo = to;
    this.isLoading = true;
    this.hasError = false;

    forkJoin({
      summary: this.statistics.getSummary(from, to),
      devices: this.statistics.getDevices(from, to)
    }).subscribe({
      next: ({ summary, devices }) => {
        if (generation !== this.loadGeneration) { return; }
        this.summary = summary;
        this.devices = devices;
        const chartDevices = devices.slice(0, CHART_DEVICE_COUNT);
        this.chartDevices = chartDevices;
        this.loadSeries(from, to, chartDevices, generation);
      },
      error: () => {
        if (generation !== this.loadGeneration) { return; }
        this.isLoading = false;
        this.hasError = true;
      }
    });
  }

  private loadSeries(from: Date, to: Date, chartDevices: EnergyDeviceStatistics[], generation: number): void {
    if (chartDevices.length === 0) {
      this.series = new Map();
      this.bars = [];
      this.isLoading = false;
      return;
    }
    forkJoin(chartDevices.map(d => this.statistics.getTimeseries(d.deviceId, from, to))).subscribe({
      next: results => {
        if (generation !== this.loadGeneration) { return; }
        this.series = new Map(chartDevices.map((d, i) => [d.deviceId, results[i]] as [number, EnergyTimeseriesPoint[]]));
        this.rebuildBars();
        this.isLoading = false;
      },
      error: () => {
        if (generation !== this.loadGeneration) { return; }
        this.isLoading = false;
        this.hasError = true;
      }
    });
  }

  private refreshLive(): void {
    this.statistics.getLive().subscribe({
      next: (live: EnergyLiveDevice[]) => {
        const power = new Map(live.map(l => [l.deviceId, l.powerW] as [number, number]));
        this.devices.forEach(d => d.currentPowerW = power.get(d.deviceId) ?? 0);
        if (this.summary) {
          this.summary.currentPowerW = live.reduce((sum, l) => sum + l.powerW, 0);
        }
        this.liveIndicator = live.length > 0;
      },
      error: () => { this.liveIndicator = false; }
    });
  }

  toggleDevice(deviceId: number): void {
    if (this.hidden.has(deviceId)) { this.hidden.delete(deviceId); } else { this.hidden.add(deviceId); }
    this.rebuildBars();
  }

  colorOf(deviceId: number): string {
    const index = this.chartDevices.findIndex(d => d.deviceId === deviceId);
    return CHART_COLORS[index >= 0 ? index : CHART_COLORS.length - 1];
  }

  sortBy(key: EnergySortKey): void {
    if (this.sortKey === key) { this.sortDescending = !this.sortDescending; } else { this.sortKey = key; this.sortDescending = key !== 'deviceName'; }
  }

  get sortedDevices(): EnergyDeviceStatistics[] {
    const factor = this.sortDescending ? -1 : 1;
    return [...this.devices].sort((a, b) => {
      const av = a[this.sortKey] ?? '';
      const bv = b[this.sortKey] ?? '';
      return (av < bv ? -1 : av > bv ? 1 : 0) * factor;
    });
  }

  select(device: EnergyDeviceStatistics): void {
    if (this.selected?.deviceId === device.deviceId) { this.selected = null; return; }
    this.selected = device;
    this.selectedBars = [];
    this.statistics.getTimeseries(device.deviceId, this.currentFrom, this.currentTo).subscribe({
      next: points => {
        if (this.selected?.deviceId === device.deviceId) {
          this.selectedBars = buildStackedBars(new Map([[device.deviceId, points]]));
        }
      }
    });
  }

  displayName(device: { deviceId: number; deviceName: string | null }): string {
    return device.deviceName || `#${device.deviceId}`;
  }

  computeRange(range: EnergyRange, now: Date): { from: Date; to: Date } {
    const from = new Date(now);
    switch (range) {
      case 'today': from.setHours(0, 0, 0, 0); break;
      case '7d': from.setDate(from.getDate() - 7); break;
      case '30d': from.setDate(from.getDate() - 30); break;
      default: from.setFullYear(from.getFullYear() - 1); break;
    }
    return { from, to: now };
  }

  private rebuildBars(): void {
    this.bars = buildStackedBars(this.series, this.hidden);
  }
}
