import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RuleOverviewDto {
  id: string;
  name: string;
  isActive: boolean;
  priority: number;
  condition?: string | null;
  action?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface RuleExecutionLogDto {
  id: number;
  ruleName: string;
  triggeredAt: string;
  success: boolean;
  errorMessage?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class RuleMonitorService {
  private readonly baseUrl = environment.api.rulesService;

  constructor(private http: HttpClient) {}

  getRules(): Observable<RuleOverviewDto[]> {
    return this.http.get<RuleOverviewDto[]>(`${this.baseUrl}/api/rules`);
  }

  getRuleExecutionLog(limit: number = 100): Observable<RuleExecutionLogDto[]> {
    return this.http.get<RuleExecutionLogDto[]>(`${this.baseUrl}/api/rules/log?limit=${limit}`);
  }
}
