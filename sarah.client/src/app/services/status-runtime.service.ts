import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StatusDto } from '../models/api-types';

@Injectable({ providedIn: 'root' })
export class StatusRuntimeService {
  private readonly baseUrl = environment.api.dashboardService;

  constructor(private http: HttpClient) {}

  statusGet(): Observable<StatusDto> {
    return this.http.get<StatusDto>(`${this.baseUrl}/Status`);
  }
}
