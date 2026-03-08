import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NetworkElementDto, TrackerDto } from '../models/api-types';
import { environment } from '../../environments/environment';

/**
 * Custom (non-generated) extension service for Device Service endpoints
 * that are not yet reflected in the generated OpenAPI client.
 */
@Injectable({
  providedIn: 'root'
})
export class DevicesExtService {
  private readonly baseUrl = environment.api.deviceService;

  constructor(private http: HttpClient) {}

  /** GET /Devices/elements — simplified list of Z-Wave network elements */
  getElements(): Observable<NetworkElementDto[]> {
    return this.http.get<NetworkElementDto[]>(`${this.baseUrl}/Devices/elements`);
  }

  /** GET /Devices/trackers — list of GPS trackers (id + name) */
  getTrackers(): Observable<TrackerDto[]> {
    return this.http.get<TrackerDto[]>(`${this.baseUrl}/Devices/trackers`);
  }

  /** POST /Devices/lamp/{id}/color/{color} */
  setLampColor(id: number, color: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/Devices/lamp/${id}/color/${encodeURIComponent(color)}`, {});
  }

  /** POST /Devices/wallplug/{id}/{isOn} */
  setWallplugState(id: number, isOn: boolean): Observable<any> {
    return this.http.post(`${this.baseUrl}/Devices/wallplug/${id}/${isOn}`, {});
  }
}
