import { of, Subject, throwError } from 'rxjs';
import { EnergyComponent } from './energy.component';
import { buildStackedBars, EnergyStatisticsService, EnergyTimeseriesPoint } from '../services/energy-statistics.service';

describe('EnergyComponent', () => {
  let statisticsSpy: jasmine.SpyObj<EnergyStatisticsService>;
  let component: EnergyComponent;

  const summary = { from: '', to: '', currentPowerW: 100, totalEnergyKwh: 3, previousEnergyKwh: 2, trendPercent: 50, pricePerKwh: 0.35, estimatedCost: 1.05, deviceCount: 2 };
  const devices = [
    { deviceId: 1, deviceName: 'Lamp', currentPowerW: 10, avgPowerW: 8, maxPowerW: 12, energyKwh: 1, sharePercent: 25 },
    { deviceId: 2, deviceName: 'Heater', currentPowerW: 90, avgPowerW: 80, maxPowerW: 100, energyKwh: 3, sharePercent: 75 }
  ];

  beforeEach(() => {
    statisticsSpy = jasmine.createSpyObj('EnergyStatisticsService', ['getSummary', 'getDevices', 'getTimeseries', 'getLive']);
    statisticsSpy.getSummary.and.returnValue(of(summary));
    statisticsSpy.getDevices.and.returnValue(of(devices));
    statisticsSpy.getTimeseries.and.returnValue(of([{ bucketStart: '2026-01-01T00:00:00Z', avgPowerW: 1, maxPowerW: 2, energyKwh: 1 }]));
    statisticsSpy.getLive.and.returnValue(of([]));
    component = new EnergyComponent(statisticsSpy);
  });

  it('loads summary, devices and chart series', () => {
    component.load();
    expect(component.summary).toEqual(summary);
    expect(component.devices.length).toBe(2);
    expect(component.bars.length).toBe(1);
    expect(component.isLoading).toBeFalse();
  });

  it('sets error state when loading fails', () => {
    statisticsSpy.getSummary.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.hasError).toBeTrue();
    expect(component.isLoading).toBeFalse();
  });

  it('ignores chart series from an older range load', () => {
    const firstDevices = [{ ...devices[0], deviceId: 1 }];
    const secondDevices = [{ ...devices[0], deviceId: 2 }];
    const responses: Subject<EnergyTimeseriesPoint[]>[] = [];
    statisticsSpy.getDevices.and.returnValues(of(firstDevices), of(secondDevices));
    statisticsSpy.getTimeseries.and.callFake(() => {
      const response = new Subject<EnergyTimeseriesPoint[]>();
      responses.push(response);
      return response;
    });

    component.load();
    component.setRange('7d');

    responses[1].next([{ bucketStart: 'new', avgPowerW: 1, maxPowerW: 1, energyKwh: 2 }]);
    responses[1].complete();
    expect(component.bars.map(bar => bar.bucketStart)).toEqual(['new']);

    responses[0].next([{ bucketStart: 'old', avgPowerW: 1, maxPowerW: 1, energyKwh: 1 }]);
    responses[0].complete();
    expect(component.bars.map(bar => bar.bucketStart)).toEqual(['new']);
    expect(component.chartDevices.map(device => device.deviceId)).toEqual([2]);
  });

  it('sorts devices by energy descending by default and toggles', () => {
    component.devices = [...devices];
    expect(component.sortedDevices[0].deviceId).toBe(2);
    component.sortBy('energyKwh');
    expect(component.sortedDevices[0].deviceId).toBe(1);
  });

  it('hiding a device removes it from the bars', () => {
    component.load();
    component.toggleDevice(1);
    component.toggleDevice(2);
    expect(component.bars.length).toBe(0);
  });

  it('computes the today range from local midnight', () => {
    const now = new Date(2026, 5, 15, 13, 30);
    const { from, to } = component.computeRange('today', now);
    expect(from.getHours()).toBe(0);
    expect(from.getDate()).toBe(15);
    expect(to).toBe(now);
  });
});

describe('buildStackedBars', () => {
  it('stacks devices per bucket relative to the highest total', () => {
    const series = new Map([
      [1, [{ bucketStart: 'a', avgPowerW: 0, maxPowerW: 0, energyKwh: 1 }, { bucketStart: 'b', avgPowerW: 0, maxPowerW: 0, energyKwh: 2 }]],
      [2, [{ bucketStart: 'a', avgPowerW: 0, maxPowerW: 0, energyKwh: 3 }]]
    ]);
    const bars = buildStackedBars(series);
    expect(bars.length).toBe(2);
    expect(bars[0].totalKwh).toBe(4);
    expect(bars[0].heightPercent).toBe(100);
    expect(bars[1].heightPercent).toBe(50);
    expect(bars[0].segments.find(s => s.deviceId === 2)!.heightPercent).toBe(75);
  });

  it('returns no bars for empty or fully hidden series', () => {
    expect(buildStackedBars(new Map()).length).toBe(0);
    const series = new Map([[1, [{ bucketStart: 'a', avgPowerW: 0, maxPowerW: 0, energyKwh: 1 }]]]);
    expect(buildStackedBars(series, new Set([1])).length).toBe(0);
  });
});
