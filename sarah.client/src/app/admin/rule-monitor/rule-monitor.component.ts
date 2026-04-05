import { Component, OnInit } from '@angular/core';
import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { RuleOverviewDtoModel } from '../../services/api/rules-service/model/ruleOverviewDto';
import { RuleExecutionLogDtoModel } from '../../services/api/rules-service/model/ruleExecutionLogDto';
import { LoggingService } from '../../services/logging.service';

@Component({
  selector: 'app-rule-monitor',
  templateUrl: './rule-monitor.component.html',
  styleUrl: './rule-monitor.component.css'
})
export class RuleMonitorComponent implements OnInit {
  rules: RuleOverviewDtoModel[] = [];
  executionLog: RuleExecutionLogDtoModel[] = [];
  loadingRules = false;
  loadingLog = false;

  constructor(
    private rulesClient: RulesClient,
    private logger: LoggingService
  ) {}

  ngOnInit(): void {
    this.loadRules();
    this.loadExecutionLog();
  }

  loadRules(): void {
    this.loadingRules = true;
    this.rulesClient.apiRulesGet().subscribe({
      next: (rules) => {
        this.rules = rules;
        this.loadingRules = false;
      },
      error: (err) => {
        this.logger.error('Fehler beim Laden der Regeln', err);
        this.loadingRules = false;
      }
    });
  }

  loadExecutionLog(): void {
    this.loadingLog = true;
    this.rulesClient.apiRulesLogGet(100).subscribe({
      next: (log) => {
        this.executionLog = log;
        this.loadingLog = false;
      },
      error: (err) => {
        this.logger.error('Fehler beim Laden des Ausführungsprotokolls', err);
        this.loadingLog = false;
      }
    });
  }
}
