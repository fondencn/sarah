import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { Observable, finalize, timeout } from 'rxjs';
import { ZWaveDiagnosticsClient } from '../../services/api/device-service/api/zWaveDiagnostics.service';
import { ZWaveNodeDiagnosticsDtoModel } from '../../services/api/device-service/model/zWaveNodeDiagnosticsDto';
import { ZWaveParameterDtoModel } from '../../services/api/device-service/model/zWaveParameterDto';
import { ZWaveModeStatusDtoModel } from '../../services/api/device-service/model/zWaveModeStatusDto';
import { ZWaveNodeStatusModel } from '../../services/api/device-service/model/zWaveNodeStatus';
import { ZWaveInclusionModeModel } from '../../services/api/device-service/model/zWaveInclusionMode';
import { LoggingService } from '../../services/logging.service';

export interface ParameterEditModel {
  id: number;
  name: string;
  size: number;
  value: number | null;
  saving: boolean;
}

export interface AssociationGroupEditModel {
  groupId: number;
  maxNodesSupported: number;
  nodesInput: string;
  saving: boolean;
}

@Component({
  selector: 'app-zwave-diagnostics',
  templateUrl: './zwave-diagnostics.component.html',
  styleUrl: './zwave-diagnostics.component.css'
})
export class ZWaveDiagnosticsComponent implements OnInit, OnDestroy {
  private readonly requestTimeoutMs = 60000;
  private readonly pollIntervalMs = 3000;
  // Reading all parameters needs one radio round trip each.
  private readonly parameterTimeoutMs = 90000;

  readonly ZWaveInclusionMode = ZWaveInclusionModeModel;

  nodes: ZWaveNodeDiagnosticsDtoModel[] = [];
  loadingNodes = false;
  controllerUnavailable = false;
  modeStatus: ZWaveModeStatusDtoModel | null = null;
  modeBusy = false;
  secondsLeft = 0;

  associationNodeId: number | null = null;
  associationGroups: AssociationGroupEditModel[] = [];
  loadingAssociations = false;

  parameterNodeId: number | null = null;
  parameters: ParameterEditModel[] = [];
  parameterDeviceType = '';
  parametersHaveProvider = true;
  loadingParameters = false;

  private pollTimer?: ReturnType<typeof setInterval>;
  private countdownTimer?: ReturnType<typeof setInterval>;
  private lastEventKey: string | null = null;

  constructor(
    private client: ZWaveDiagnosticsClient,
    private logger: LoggingService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.refreshMode();
    this.loadNodes();
    this.pollTimer = setInterval(() => this.refreshMode(), this.pollIntervalMs);
    this.countdownTimer = setInterval(() => this.updateCountdown(), 1000);
  }

  ngOnDestroy(): void {
    clearInterval(this.pollTimer);
    clearInterval(this.countdownTimer);
  }

  get isInclusion(): boolean {
    return this.modeStatus?.mode === ZWaveInclusionModeModel.NUMBER_1;
  }

  get isExclusion(): boolean {
    return this.modeStatus?.mode === ZWaveInclusionModeModel.NUMBER_2;
  }

  get modeActive(): boolean {
    return this.isInclusion || this.isExclusion;
  }

  statusKey(status?: ZWaveNodeStatusModel): string {
    switch (status) {
      case ZWaveNodeStatusModel.NUMBER_0: return 'alive';
      case ZWaveNodeStatusModel.NUMBER_1: return 'sleeping';
      case ZWaveNodeStatusModel.NUMBER_2: return 'failed';
      default: return 'unreachable';
    }
  }

  statusBadgeClass(status?: ZWaveNodeStatusModel): string {
    switch (status) {
      case ZWaveNodeStatusModel.NUMBER_0: return 'bg-success';
      case ZWaveNodeStatusModel.NUMBER_1: return 'bg-info text-dark';
      case ZWaveNodeStatusModel.NUMBER_2: return 'bg-danger';
      default: return 'bg-secondary';
    }
  }

  loadNodes(): void {
    this.loadingNodes = true;
    this.client.zWaveDiagnosticsGetNodesGETApiDevicesZwaveDiagnosticsNodes().pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => { this.loadingNodes = false; })
    ).subscribe({
      next: (nodes) => {
        this.controllerUnavailable = false;
        this.nodes = nodes;
      },
      error: (err) => this.handleError('zwaveDiagnostics.errors.loadNodes', err)
    });
  }

  refreshMode(): void {
    this.client.zWaveDiagnosticsGetModeGETApiDevicesZwaveDiagnosticsMode().pipe(
      timeout(this.requestTimeoutMs)
    ).subscribe({
      next: (status) => this.applyModeStatus(status),
      error: () => { /* polling failures stay silent; explicit actions report errors */ }
    });
  }

  startInclusion(): void {
    this.runModeAction(this.client.zWaveDiagnosticsStartInclusionPOSTApiDevicesZwaveDiagnosticsInclusionStart());
  }

  startExclusion(): void {
    this.runModeAction(this.client.zWaveDiagnosticsStartExclusionPOSTApiDevicesZwaveDiagnosticsExclusionStart());
  }

  stopMode(): void {
    this.runModeAction(this.client.zWaveDiagnosticsStopModePOSTApiDevicesZwaveDiagnosticsExclusionStop());
  }

  openAssociations(node: ZWaveNodeDiagnosticsDtoModel): void {
    if (!node.isKnown || node.nodeId == null) {
      return;
    }
    if (this.associationNodeId === node.nodeId) {
      this.closeAssociations();
      return;
    }
    this.associationNodeId = node.nodeId;
    this.associationGroups = [];
    this.loadingAssociations = true;
    this.client.zWaveDiagnosticsGetAssociationGroupsGETApiDevicesZwaveDiagnosticsNodesNodeIdAssociationGroups(node.nodeId).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => { this.loadingAssociations = false; })
    ).subscribe({
      next: (groups) => {
        this.associationGroups = groups.map(g => ({
          groupId: g.groupId ?? 0,
          maxNodesSupported: g.maxNodesSupported ?? 0,
          nodesInput: (g.nodes ?? []).join(', '),
          saving: false
        }));
      },
      error: (err) => this.handleError('zwaveDiagnostics.errors.loadAssociations', err)
    });
  }

  openParameters(node: ZWaveNodeDiagnosticsDtoModel): void {
    if (!node.isKnown || node.nodeId == null) {
      return;
    }
    if (this.parameterNodeId === node.nodeId) {
      this.closeParameters();
      return;
    }
    this.parameterNodeId = node.nodeId;
    this.parameters = [];
    this.parametersHaveProvider = true;
    this.loadingParameters = true;
    this.client.zWaveDiagnosticsGetParametersGETApiDevicesZwaveDiagnosticsNodesNodeIdParameters(node.nodeId).pipe(
      timeout(this.parameterTimeoutMs),
      finalize(() => { this.loadingParameters = false; })
    ).subscribe({
      next: (result) => {
        this.parameterDeviceType = result.deviceType ?? '';
        this.parametersHaveProvider = result.hasProvider ?? false;
        this.parameters = (result.parameters ?? []).map(p => this.toEditModel(p));
      },
      error: (err) => this.handleError('zwaveDiagnostics.errors.loadParameters', err)
    });
  }

  closeParameters(): void {
    this.parameterNodeId = null;
    this.parameters = [];
  }

  saveParameter(parameter: ParameterEditModel): void {
    if (this.parameterNodeId == null) {
      return;
    }
    if (!this.isValidParameterValue(parameter.value)) {
      this.logger.warn(this.translate.instant('zwaveDiagnostics.errors.invalidParameterValue'));
      return;
    }
    parameter.saving = true;
    this.client.zWaveDiagnosticsSetParameterPUTApiDevicesZwaveDiagnosticsNodesNodeIdParametersParamId(
      this.parameterNodeId, parameter.id, { value: parameter.value as number }
    ).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => { parameter.saving = false; })
    ).subscribe({
      // The backend reads the value back from the device, so show what the device reports.
      next: (updated) => { parameter.value = updated.value ?? parameter.value; },
      error: (err) => this.handleError('zwaveDiagnostics.errors.saveParameter', err)
    });
  }

  /** The backend writes a single byte. */
  isValidParameterValue(value: number | null): boolean {
    return value != null && Number.isInteger(value) && value >= 0 && value <= 255;
  }

  private toEditModel(p: ZWaveParameterDtoModel): ParameterEditModel {
    return { id: p.id ?? 0, name: p.name ?? '', size: p.size ?? 0, value: p.value ?? null, saving: false };
  }

  closeAssociations(): void {
    this.associationNodeId = null;
    this.associationGroups = [];
  }

  saveAssociationGroup(group: AssociationGroupEditModel): void {
    if (this.associationNodeId == null) {
      return;
    }
    const nodeIds = this.parseNodeIds(group.nodesInput);
    if (nodeIds == null) {
      this.logger.warn(this.translate.instant('zwaveDiagnostics.errors.invalidNodeIds'));
      return;
    }
    group.saving = true;
    this.client.zWaveDiagnosticsSetAssociationGroupPUTApiDevicesZwaveDiagnosticsNodesNodeIdAssociationGroupsGroupId(
      this.associationNodeId, group.groupId, { nodeIds }
    ).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => { group.saving = false; })
    ).subscribe({
      next: () => { group.nodesInput = nodeIds.join(', '); },
      error: (err) => this.handleError('zwaveDiagnostics.errors.saveAssociation', err)
    });
  }

  /** Returns null when the input contains anything but node ids between 1 and 232. */
  parseNodeIds(input: string): number[] | null {
    const parts = input.split(/[\s,;]+/).filter(p => p.length > 0);
    const ids: number[] = [];
    for (const part of parts) {
      const id = Number(part);
      if (!Number.isInteger(id) || id < 1 || id > 232) {
        return null;
      }
      if (!ids.includes(id)) {
        ids.push(id);
      }
    }
    return ids;
  }

  private runModeAction(request$: Observable<ZWaveModeStatusDtoModel>): void {
    this.modeBusy = true;
    request$.pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => { this.modeBusy = false; })
    ).subscribe({
      next: (status) => this.applyModeStatus(status),
      error: (err) => this.handleError('zwaveDiagnostics.errors.modeAction', err)
    });
  }

  private applyModeStatus(status: ZWaveModeStatusDtoModel): void {
    this.modeStatus = status;
    this.updateCountdown();

    const event = status.lastEvent;
    const key = event ? `${event.nodeId}|${event.isNodeAdded}|${event.timestamp}` : '';
    const isFirstStatus = this.lastEventKey === null;
    const changed = key !== '' && key !== this.lastEventKey;
    this.lastEventKey = key;
    // A node was included/excluded while we watched: refresh the list (the initial load already covers older events).
    if (changed && !isFirstStatus) {
      this.loadNodes();
    }
  }

  private updateCountdown(): void {
    const expiresAt = this.modeStatus?.expiresAt;
    this.secondsLeft = this.modeActive && expiresAt
      ? Math.max(0, Math.round((new Date(expiresAt).getTime() - Date.now()) / 1000))
      : 0;
  }

  private handleError(messageKey: string, err: unknown): void {
    if (err instanceof HttpErrorResponse && err.status === 503) {
      this.controllerUnavailable = true;
      return;
    }
    this.logger.error(this.translate.instant(messageKey), err);
  }
}
