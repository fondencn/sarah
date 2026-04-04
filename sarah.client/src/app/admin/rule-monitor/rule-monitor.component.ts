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
  logEntries: RuleExecutionLogDto[] = [];
  errorsOnly = false;
  loading = false;
  expandedLogId: number | null = null;

  constructor(
    private ruleMonitor: RuleMonitorService,
    private logger: LoggingService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;

    this.ruleMonitor.getRules().subscribe({
      next: rules => {
        this.rules = rules;
      },
      error: () => {
        this.logger.error('Regeln konnten nicht geladen werden');
        this.rules = [];
      }
    });

    this.ruleMonitor.getRuleLog(100, this.errorsOnly).subscribe({
      next: entries => {
        this.logEntries = entries;
        this.loading = false;
      },
      error: () => {
        this.logger.error('Ausführungsprotokoll konnte nicht geladen werden');
        this.logEntries = [];
        this.loading = false;
      }
    });
  }

  onErrorsOnlyChange(): void {
    this.load();
  }

  toggleDetail(id: number): void {
    this.expandedLogId = this.expandedLogId === id ? null : id;
  }

  statusIcon(rule: RuleOverviewDto): string {
    if (rule.lastSuccess === null || rule.lastSuccess === undefined) return '⚪';
    return rule.lastSuccess ? '✅' : '❌';
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return 'Noch nie ausgeführt';
    const d = new Date(dateStr);
    return d.toLocaleString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
