import { Component, OnInit } from '@angular/core';
import { RuleMonitorService, RuleOverviewDto, RuleExecutionLogDto } from '../../services/rule-monitor.service';
import { LoggingService } from '../../services/logging.service';

@Component({
  selector: 'app-rule-monitor',
  templateUrl: './rule-monitor.component.html',
  styleUrl: './rule-monitor.component.css'
})
export class RuleMonitorComponent implements OnInit {
  rules: RuleOverviewDto[] = [];
  executionLog: RuleExecutionLogDto[] = [];
  loadingRules = false;
  loadingLog = false;

  constructor(
    private ruleMonitorService: RuleMonitorService,
    private logger: LoggingService
  ) {}

  ngOnInit(): void {
    this.loadRules();
    this.loadExecutionLog();
  }

  loadRules(): void {
    this.loadingRules = true;
    this.ruleMonitorService.getRules().subscribe({
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
    this.ruleMonitorService.getRuleExecutionLog().subscribe({
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
