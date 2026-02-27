import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Custom (non-generated) extension service for Persons Service endpoints
 * that are not yet reflected in the generated OpenAPI client.
 */
@Injectable({
  providedIn: 'root'
})
export class PersonsExtService {
  private readonly baseUrl = environment.api.personsService;

  constructor(private http: HttpClient) {}

  /** GET /api/Persons/known-home-network-devices — list of all known home network device hostnames */
  getKnownHomeNetworkDevices(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/api/Persons/known-home-network-devices`);
  }
}
