import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MonitoringControllerService } from './api/monitoring-service';

/**
 * Wrapper service for Monitoring Service API
 * Provides a simplified interface to the generated Monitoring Service API client
 */
@Injectable({
  providedIn: 'root'
})
export class MonitoringService {

  constructor(private monitoringClient: MonitoringControllerService) { }

  /**
   * Get current weather data
   */
  getWeather(): Observable<any> {
    return this.monitoringClient.getWeather();
  }

  /**
   * Get vacation (Ferien) information
   */
  getFerien(): Observable<any> {
    return this.monitoringClient.getFerien();
  }

  /**
   * Get monitoring service status
   */
  getStatus(): Observable<any> {
    return this.monitoringClient.getStatus();
  }
}
