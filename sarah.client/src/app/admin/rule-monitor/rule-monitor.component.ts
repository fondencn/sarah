import { Component, OnDestroy, OnInit } from '@angular/core';
import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { RuleOverviewDtoModel } from '../../services/api/rules-service/model/ruleOverviewDto';
import { RuleExecutionLogDtoModel } from '../../services/api/rules-service/model/ruleExecutionLogDto';
import { LoggingService } from '../../services/logging.service';
import { finalize, timeout } from 'rxjs';
import { OAuthService } from 'angular-oauth2-oidc';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-rule-monitor',
  templateUrl: './rule-monitor.component.html',
  styleUrl: './rule-monitor.component.css'
})
export class RuleMonitorComponent implements OnInit, OnDestroy {
  private readonly requestTimeoutMs = 10000;
  private tokenEventsSubscription: Subscription | null = null;

  rules: RuleOverviewDtoModel[] = [];
  executionLog: RuleExecutionLogDtoModel[] = [];
  loadingRules = false;
  loadingLog = false;

  constructor(
    private rulesClient: RulesClient,
    private logger: LoggingService,
    private oauthService: OAuthService
  ) {}

  ngOnInit(): void {
    if (this.oauthService.hasValidAccessToken()) {
      this.loadRules();
      this.loadExecutionLog();
      return;
    }

    // Initial route rendering can happen before token persistence completes.
    // Wait for token arrival and then perform the first fetch.
    this.tokenEventsSubscription = this.oauthService.events.subscribe((event) => {
      if (event.type === 'token_received' && this.oauthService.hasValidAccessToken()) {
        this.loadRules();
        this.loadExecutionLog();
      }
    });
  }

  ngOnDestroy(): void {
    this.tokenEventsSubscription?.unsubscribe();
    this.tokenEventsSubscription = null;
  }

  loadRules(): void {
    this.loadingRules = true;
    this.rulesClient.apiRulesGet().pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.loadingRules = false;
      })
    ).subscribe({
      next: (rules) => {
        this.rules = rules;
      },
      error: (err) => {
        this.logger.error('Fehler beim Laden der Regeln', err);
      }
    });
  }

  loadExecutionLog(): void {
    this.loadingLog = true;
    this.rulesClient.apiRulesLogGet(100).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.loadingLog = false;
      })
    ).subscribe({
      next: (log) => {
        this.executionLog = log;
      },
      error: (err) => {
        this.logger.error('Fehler beim Laden des Ausführungsprotokolls', err);
      }
    });
  }
}
