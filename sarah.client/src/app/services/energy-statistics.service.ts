import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface EnergySummary {
  from: string;
  to: string;
  currentPowerW: number;
  totalEnergyKwh: number;
  previousEnergyKwh: number;
  trendPercent: number | null;
  pricePerKwh: number;
  estimatedCost: number;
  deviceCount: number;
}

export interface EnergyDeviceStatistics {
  deviceId: number;
  deviceName: string | null;
  currentPowerW: number;
  avgPowerW: number;
  maxPowerW: number;
  energyKwh: number;
  sharePercent: number;
}

export interface EnergyTimeseriesPoint {
  bucketStart: string;
  avgPowerW: number;
  maxPowerW: number;
  energyKwh: number;
}

export interface EnergyLiveDevice {
  deviceId: number;
  deviceName: string | null;
  powerW: number;
  timestamp: string;
}

export interface StackedSegment { deviceId: number; kwh: number; heightPercent: number; }
export interface StackedBar { bucketStart: string; totalKwh: number; heightPercent: number; segments: StackedSegment[]; }

/**
 * Builds stacked bars (one per time bucket) from per-device time series.
 * Heights are relative to the highest bucket total.
 */
export function buildStackedBars(series: Map<number, EnergyTimeseriesPoint[]>, hidden: ReadonlySet<number> = new Set()): StackedBar[] {
  const buckets = new Map<string, Map<number, number>>();
  series.forEach((points, deviceId) => {
    if (hidden.has(deviceId)) { return; }
    for (const p of points) {
      const perDevice = buckets.get(p.bucketStart) ?? new Map<number, number>();
      perDevice.set(deviceId, (perDevice.get(deviceId) ?? 0) + p.energyKwh);
      buckets.set(p.bucketStart, perDevice);
    }
  });

  const totals = [...buckets.entries()].map(([bucketStart, perDevice]) => ({
    bucketStart, perDevice, total: [...perDevice.values()].reduce((a, b) => a + b, 0)
  })).sort((a, b) => a.bucketStart.localeCompare(b.bucketStart));
  const max = Math.max(0, ...totals.map(t => t.total));

  return totals.map(t => ({
    bucketStart: t.bucketStart,
    totalKwh: t.total,
    heightPercent: max > 0 ? t.total / max * 100 : 0,
    segments: [...t.perDevice.entries()].map(([deviceId, kwh]) => ({
      deviceId, kwh, heightPercent: t.total > 0 ? kwh / t.total * 100 : 0
    }))
  }));
}

/** Hand-written client for the Statistics service (not yet part of the generated OpenAPI clients). */
@Injectable({
  providedIn: 'root'
})
export class EnergyStatisticsService {
  private readonly baseUrl = environment.api.statisticsService;

  constructor(private http: HttpClient) {}

  getSummary(from: Date, to: Date): Observable<EnergySummary> {
    return this.http.get<EnergySummary>(`${this.baseUrl}/api/statistics/energy/summary`, { params: this.range(from, to) });
  }

  getDevices(from: Date, to: Date, granularity?: number): Observable<EnergyDeviceStatistics[]> {
    return this.http.get<EnergyDeviceStatistics[]>(`${this.baseUrl}/api/statistics/energy/devices`, { params: this.range(from, to, granularity) });
  }

  getTimeseries(deviceId: number, from: Date, to: Date, granularity?: number): Observable<EnergyTimeseriesPoint[]> {
    return this.http.get<EnergyTimeseriesPoint[]>(`${this.baseUrl}/api/statistics/energy/devices/${deviceId}/timeseries`, { params: this.range(from, to, granularity) });
  }

  getLive(): Observable<EnergyLiveDevice[]> {
    return this.http.get<EnergyLiveDevice[]>(`${this.baseUrl}/api/statistics/energy/live`);
  }

  private range(from: Date, to: Date, granularity?: number): HttpParams {
    let params = new HttpParams().set('from', from.toISOString()).set('to', to.toISOString());
    if (granularity !== undefined) {
      params = params.set('granularity', granularity);
    }
    return params;
  }
}
