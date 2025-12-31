import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GeofencesControllerService } from './api/geofences-service';

/**
 * Wrapper service for Geofences Service API
 * Provides a simplified interface to the generated Geofences Service API client
 */
@Injectable({
  providedIn: 'root'
})
export class GeofenceService {

  constructor(private geofencesClient: GeofencesControllerService) { }

  /**
   * Get current geofence by coordinates
   * @param lat Latitude
   * @param lon Longitude
   */
  getCurrentGeofence(lat: number, lon: number): Observable<any> {
    return this.geofencesClient.getCurrentGeofence(lat, lon);
  }

  /**
   * Get home geofence
   */
  getHomeGeofence(): Observable<any> {
    return this.geofencesClient.getHomeGeofence();
  }

  /**
   * Get geofences service status
   */
  getStatus(): Observable<any> {
    return this.geofencesClient.getStatus();
  }
}
