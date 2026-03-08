import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LocationDto, NamedLocationDto } from '../models/api-types';

@Injectable({ providedIn: 'root' })
export class LocationRuntimeService {
  private readonly baseUrl = environment.api.geofencesService;

  constructor(private http: HttpClient) {}

  apiLocationTrackerIdGet(id: number): Observable<LocationDto> {
    return this.http.get<LocationDto>(`${this.baseUrl}/api/Location/tracker/${id}`);
  }

  apiLocationWellknownlocationsGet(): Observable<NamedLocationDto[]> {
    return this.http.get<NamedLocationDto[]>(`${this.baseUrl}/api/Location/wellknownlocations`);
  }
}
