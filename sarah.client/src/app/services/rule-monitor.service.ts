import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoggingService } from './logging.service';

export interface RuleOverviewDto {
  name: string;
  condition: string | null;
  action: string | null;
  isActive: boolean;
  priority: number;
  lastExecution: string | null;
  lastSuccess: boolean | null;
}

export interface RuleExecutionLogDto {
  id: number;
  ruleName: string;
  executedAt: string;
  success: boolean;
  errorMessage: string | null;
  triggerEventType: string | null;
}

@Injectable({ providedIn: 'root' })
export class RuleMonitorService {
  private readonly baseUrl = environment.api.rulesService;

  constructor(private http: HttpClient, private logger: LoggingService) {}

  getRules(): Observable<RuleOverviewDto[]> {
    return this.http.get<RuleOverviewDto[]>(`${this.baseUrl}/api/rules`).pipe(
      tap(() => this.logger.debug('Loaded rule overview')),
      catchError(err => {
        this.logger.error('Fehler beim Laden der Regeln', err);
        return throwError(() => err);
      })
    );
  }

  getRuleLog(limit: number = 100, errorsOnly: boolean = false): Observable<RuleExecutionLogDto[]> {
    return this.http
      .get<RuleExecutionLogDto[]>(
        `${this.baseUrl}/api/rules/log?limit=${limit}&errorsOnly=${errorsOnly}`
      )
      .pipe(
        tap(() => this.logger.debug('Loaded rule execution log')),
        catchError(err => {
          this.logger.error('Fehler beim Laden des Ausführungsprotokolls', err);
          return throwError(() => err);
        })
      );
  }
}
