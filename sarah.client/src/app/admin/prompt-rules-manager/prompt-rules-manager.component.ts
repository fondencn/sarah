import { Component, OnInit } from '@angular/core';
import { finalize, timeout } from 'rxjs';
import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { PromptRuleDtoModel } from '../../services/api/rules-service/model/promptRuleDto';
import { PromptRuleTimerDtoModel } from '../../services/api/rules-service/model/promptRuleTimerDto';
import { PromptRuleUpsertDtoModel } from '../../services/api/rules-service/model/promptRuleUpsertDto';
import { LoggingService } from '../../services/logging.service';

interface PromptRuleFormModel {
  id?: number;
  name: string;
  guidance: string;
  isEnabled: boolean;
  sortOrder: number;
  hasTimer: boolean;
  timerHour: number;
  timerMinute: number;
  timerInterval: number;
  timerWeekdays: number;
}

@Component({
  selector: 'app-prompt-rules-manager',
  templateUrl: './prompt-rules-manager.component.html',
  styleUrl: './prompt-rules-manager.component.css'
})
export class PromptRulesManagerComponent implements OnInit {
  private readonly requestTimeoutMs = 10000;

  promptRules: PromptRuleDtoModel[] = [];
  loadingRules = false;
  saving = false;
  deletingRuleId: number | null = null;
  editingRuleId: number | null = null;
  showCreateForm = false;

  formModel: PromptRuleFormModel = this.createDefaultFormModel();

  constructor(
    private rulesClient: RulesClient,
    private logger: LoggingService
  ) {}

  ngOnInit(): void {
    this.loadPromptRules();
  }

  loadPromptRules(): void {
    this.loadingRules = true;

    this.rulesClient.apiRulesPromptRulesGet().pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.loadingRules = false;
      })
    ).subscribe({
      next: (rules) => {
        this.promptRules = rules;
      },
      error: (err) => {
        this.logger.error('Fehler beim Laden der Prompt-Regeln', err);
      }
    });
  }

  startCreate(): void {
    this.showCreateForm = true;
    this.editingRuleId = null;
    this.formModel = this.createDefaultFormModel();
  }

  startEdit(rule: PromptRuleDtoModel): void {
    this.showCreateForm = false;
    this.editingRuleId = rule.id ?? null;

    const timer = rule.timer;
    this.formModel = {
      id: rule.id,
      name: rule.name ?? '',
      guidance: rule.guidance ?? '',
      isEnabled: rule.isEnabled ?? true,
      sortOrder: rule.sortOrder ?? 0,
      hasTimer: !!timer,
      timerHour: timer?.hour ?? 12,
      timerMinute: timer?.minute ?? 0,
      timerInterval: timer?.interval ?? 0,
      timerWeekdays: timer?.weekdays ?? this.defaultWeekdays()
    };
  }

  cancelEdit(): void {
    this.showCreateForm = false;
    this.editingRuleId = null;
    this.formModel = this.createDefaultFormModel();
  }

  save(): void {
    if (!this.formModel.name.trim() || !this.formModel.guidance.trim()) {
      return;
    }

    const payload: PromptRuleUpsertDtoModel = {
      name: this.formModel.name.trim(),
      guidance: this.formModel.guidance.trim(),
      isEnabled: this.formModel.isEnabled,
      sortOrder: this.formModel.sortOrder,
      timer: this.buildTimerPayload()
    };

    this.saving = true;

    const request$ = this.editingRuleId != null
      ? this.rulesClient.apiRulesPromptRulesIdPut(this.editingRuleId, payload)
      : this.rulesClient.apiRulesPromptRulesPost(payload);

    request$.pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.saving = false;
      })
    ).subscribe({
      next: () => {
        this.cancelEdit();
        this.loadPromptRules();
      },
      error: (err) => {
        this.logger.error('Fehler beim Speichern der Prompt-Regel', err);
      }
    });
  }

  delete(rule: PromptRuleDtoModel): void {
    if (rule.id == null) {
      return;
    }

    this.deletingRuleId = rule.id;

    this.rulesClient.apiRulesPromptRulesIdDelete(rule.id).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.deletingRuleId = null;
      })
    ).subscribe({
      next: () => {
        if (this.editingRuleId === rule.id) {
          this.cancelEdit();
        }

        this.loadPromptRules();
      },
      error: (err) => {
        this.logger.error('Fehler beim Loeschen der Prompt-Regel', err);
      }
    });
  }

  onTimerEnabledChanged(): void {
    if (!this.formModel.hasTimer) {
      return;
    }

    if (!this.formModel.timerWeekdays) {
      this.formModel.timerWeekdays = this.defaultWeekdays();
    }
  }

  toggleWeekday(flag: number, checked: boolean): void {
    if (checked) {
      this.formModel.timerWeekdays |= flag;
      return;
    }

    this.formModel.timerWeekdays &= ~flag;
  }

  isWeekdayChecked(flag: number): boolean {
    return (this.formModel.timerWeekdays & flag) === flag;
  }

  private buildTimerPayload(): PromptRuleTimerDtoModel | undefined {
    if (!this.formModel.hasTimer) {
      return undefined;
    }

    return {
      hour: this.formModel.timerHour,
      minute: this.formModel.timerMinute,
      weekdays: this.formModel.timerWeekdays,
      interval: this.formModel.timerInterval,
      fromUtc: undefined,
      untilUtc: undefined
    };
  }

  private createDefaultFormModel(): PromptRuleFormModel {
    const nextSortOrder = this.promptRules.length > 0
      ? Math.max(...this.promptRules.map(rule => rule.sortOrder ?? 0)) + 1
      : 1;

    return {
      id: undefined,
      name: '',
      guidance: '',
      isEnabled: true,
      sortOrder: nextSortOrder,
      hasTimer: false,
      timerHour: 12,
      timerMinute: 0,
      timerInterval: 0,
      timerWeekdays: this.defaultWeekdays()
    };
  }

  private defaultWeekdays(): number {
    return 1 | 2 | 4 | 8 | 16;
  }
}
