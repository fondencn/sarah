import { Component, OnDestroy } from '@angular/core';
import { Modal } from 'bootstrap';
import { DevicesClient } from '../../services/api/device-service/api/devices.service';
import { LoggingService } from '../../services/logging.service';

@Component({
  selector: 'app-device-control-modal',
  templateUrl: './device-control-modal.component.html',
  styleUrls: ['./device-control-modal.component.css']
})
export class DeviceControlModalComponent implements OnDestroy {
  private readonly lampOnBrightness = 255;

  deviceId: number = 0;
  deviceName: string = '';
  typeName: string = '';
  isVisible: boolean = false;

  // State properties derived from extendedProperties
  isOn: boolean = false;
  lampColor: string = '#ffffff';
  lampBrightness: number = 0;
  powerConsumption: number = 0;
  thermostatTemperature: number | null = null;
  thermostatSetpoint: number | null = null;
  thermostatBattery: number | null = null;
  doorIsOpen: boolean = false;
  airTemperature: number | null = null;
  airHumidity: number | null = null;
  airCO2: number | null = null;
  airVOC: number | null = null;
  motionPresence: boolean = false;
  motionLuminance: number | null = null;
  motionBattery: number | null = null;

  private bootstrapModal: any = null;
  private refreshInterval: any = null;
  private readonly REFRESH_MS = 3000;

  constructor(
    private devicesService: DevicesClient,
    private logger: LoggingService
  ) {}

  ngOnDestroy(): void {
    this.stopRefresh();
  }

  show(deviceId: number, deviceName: string, typeName: string): void {
    this.deviceId = deviceId;
    this.deviceName = deviceName;
    this.typeName = typeName;
    this.isVisible = true;
    this.loadState();

    setTimeout(() => {
      const modalEl = document.getElementById('deviceControlModal');
      if (modalEl) {
        this.bootstrapModal = new Modal(modalEl);
        this.bootstrapModal.show();
        modalEl.addEventListener('hidden.bs.modal', () => {
          this.isVisible = false;
          this.stopRefresh();
          this.bootstrapModal = null;
        }, { once: true });
        this.startRefresh();
      }
    });
  }

  hide(): void {
    if (this.bootstrapModal) {
      this.bootstrapModal.hide();
    }
  }

  toggleLamp(eventTarget: EventTarget | null): void {
    const el = eventTarget as HTMLInputElement;
    if (el.checked) {
      this.setLampBrightness(this.lampOnBrightness);
    } else {
      this.setLampBrightness(0);
    }
  }

  setLampBrightness(brightness: number): void {
    this.devicesService.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness(this.deviceId, brightness).subscribe({
      next: () => {
        this.isOn = brightness > 0;
        this.lampBrightness = brightness;
      },
      error: err => this.logger.error('Error setting lamp brightness:', err)
    });
  }

  setLampColor(eventTarget: EventTarget | null): void {
    const el = eventTarget as HTMLInputElement;
    this.devicesService.devicesSetLampColorPOSTApiDevicesLampIdColorColor(this.deviceId, el.value).subscribe({
      next: () => { this.lampColor = el.value; },
      error: err => this.logger.error('Error setting lamp color:', err)
    });
  }

  setLampWarmWhite(): void {
    this.devicesService.devicesSetLampWarmWhitePOSTApiDevicesLampIdWarmwhite(this.deviceId).subscribe({
      error: err => this.logger.error('Error setting lamp warm white:', err)
    });
  }

  setLampColdWhite(): void {
    this.devicesService.devicesSetLampColdWhitePOSTApiDevicesLampIdColdwhite(this.deviceId).subscribe({
      error: err => this.logger.error('Error setting lamp cold white:', err)
    });
  }

  toggleWallplug(eventTarget: EventTarget | null): void {
    const el = eventTarget as HTMLInputElement;
    this.devicesService.devicesSetWallplugStateByDeviceIdPOSTApiDevicesWallplugIdStateIsOn(this.deviceId, el.checked).subscribe({
      next: () => { this.isOn = el.checked; },
      error: err => this.logger.error('Error toggling wallplug:', err)
    });
  }

  setThermostatTemperatureValue(itemId: number | undefined, temperature: number): void {
    if (itemId === undefined || isNaN(temperature)) return;
    this.devicesService.devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature(itemId, temperature).subscribe({
      next: () => { this.thermostatSetpoint = temperature; },
      error: err => this.logger.error('Error setting thermostat temperature:', err)
    });
  }

  private loadState(): void {
    this.devicesService.devicesGetDeviceByIdGETApiDevicesId(this.deviceId).subscribe({
      next: (dev: any) => { this.updateStateFromDevice(dev); },
      error: err => this.logger.error('Error loading device state:', err)
    });
  }

  private updateStateFromDevice(dev: any): void {
    const props = dev.extendedProperties as Array<{ key: string; value: string }> | null | undefined;
    const getProp = (key: string) => props?.find(p => p.key === key)?.value;

    if (this.typeName === 'Lampe') {
      this.isOn = getProp('IsOn') === 'True';
      this.lampColor = getProp('Color') ?? '#ffffff';
      this.lampBrightness = this.parseNumberOrDefault(getProp('Brightness'), 0);
    } else if (this.typeName === 'Steckdose') {
      this.isOn = getProp('IsOn') === 'True';
      this.powerConsumption = this.parseNumberOrDefault(getProp('Meter_W'), 0);
    } else if (this.typeName === 'Heizung') {
      this.thermostatTemperature = this.parseNumber(getProp('Temperature'));
      this.thermostatSetpoint = this.parseNumber(getProp('TemperatureSetpoint'));
      this.thermostatBattery = this.parseNumber(getProp('Battery'));
    } else if (this.typeName === 'DoorSensor') {
      this.doorIsOpen = getProp('IsOpen') === 'True';
    } else if (this.typeName === 'EutronicAirQualitySensor') {
      this.airTemperature = this.parseNumber(getProp('Temperature'));
      this.airHumidity = this.parseNumber(getProp('RelativeHumidity'));
      this.airCO2 = this.parseNumber(getProp('CO2'));
      this.airVOC = this.parseNumber(getProp('VOC'));
    } else if (this.typeName === 'MotionSensor') {
      this.motionPresence = getProp('Presence') === 'True';
      this.motionLuminance = this.parseNumber(getProp('Luminance'));
      this.motionBattery = this.parseNumber(getProp('Battery'));
      this.airTemperature = this.parseNumber(getProp('Temperature'));
    }
  }

  private parseNumber(val: string | undefined): number | null {
    if (val === undefined || val === null || val === '') return null;
    const n = Number(val);
    return Number.isFinite(n) ? n : null;
  }

  private parseNumberOrDefault(val: string | undefined, defaultValue: number): number {
    const n = this.parseNumber(val);
    return n !== null ? n : defaultValue;
  }

  private startRefresh(): void {
    if (this.refreshInterval) return;
    this.refreshInterval = setInterval(() => {
      if (this.isVisible) {
        this.loadState();
      }
    }, this.REFRESH_MS);
  }

  private stopRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }
}
