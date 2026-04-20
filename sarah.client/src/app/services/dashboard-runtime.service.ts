import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateDashboardItemDto, DashboardItemDto, DashboardItemType, ReorderDashboardItemsDto } from '../models/api-types';

@Injectable({ providedIn: 'root' })
export class DashboardRuntimeService {
  private readonly baseUrl = environment.api.dashboardService;

  constructor(private http: HttpClient) {}

  apiDashboardGet(): Observable<DashboardItemDto[]> {
    return this.http.get<DashboardItemDto[]>(`${this.baseUrl}/api/Dashboard`);
  }

  apiDashboardPost(dto: CreateDashboardItemDto): Observable<DashboardItemDto> {
    return this.http.post<DashboardItemDto>(`${this.baseUrl}/api/Dashboard`, dto);
  }

  apiDashboardItemIdItemTypeDelete(itemId: number, itemType: DashboardItemType): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/Dashboard/${itemId}/${itemType}`);
  }

  apiDashboardReorderPut(dto: ReorderDashboardItemsDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/Dashboard/reorder`, dto);
  }
}
