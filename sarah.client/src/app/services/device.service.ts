import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DevicesControllerService } from './api/device-service';

/**
 * Wrapper service for Device Service API
 * Provides a simplified interface to the generated Device Service API client
 */
@Injectable({
  providedIn: 'root'
})
export class DeviceService {

  constructor(private deviceClient: DevicesControllerService) { }

  /**
   * Get all lamps in the system
   */
  getLamps(): Observable<any> {
    return this.deviceClient.getLamps();
  }

  /**
   * Get all sensors (multi-sensors) in the system
   */
  getSensors(): Observable<any> {
    return this.deviceClient.getSensors();
  }

  /**
   * Get all door sensors in the system
   */
  getDoorSensors(): Observable<any> {
    return this.deviceClient.getDoorSensors();
  }

  /**
   * Get all wall plugs in the system
   */
  getWallPlugs(): Observable<any> {
    return this.deviceClient.getWallPlugs();
  }

  /**
   * Get all thermostats in the system
   */
  getThermostats(): Observable<any> {
    return this.deviceClient.getThermostats();
  }

  /**
   * Get device service status
   */
  getStatus(): Observable<any> {
    return this.deviceClient.getStatus();
  }
}
