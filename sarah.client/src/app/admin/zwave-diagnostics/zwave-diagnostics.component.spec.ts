import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { ZWaveDiagnosticsComponent } from './zwave-diagnostics.component';
import { ZWaveDiagnosticsClient } from '../../services/api/device-service/api/zWaveDiagnostics.service';
import { ZWaveInclusionModeModel } from '../../services/api/device-service/model/zWaveInclusionMode';
import { ZWaveNodeStatusModel } from '../../services/api/device-service/model/zWaveNodeStatus';
import { LoggingService } from '../../services/logging.service';

describe('ZWaveDiagnosticsComponent', () => {
  let fixture: ComponentFixture<ZWaveDiagnosticsComponent>;
  let component: ZWaveDiagnosticsComponent;
  let client: jasmine.SpyObj<ZWaveDiagnosticsClient>;

  beforeEach(async () => {
    client = jasmine.createSpyObj<ZWaveDiagnosticsClient>('ZWaveDiagnosticsClient', [
      'zWaveDiagnosticsGetNodesGETApiDevicesZwaveDiagnosticsNodes',
      'zWaveDiagnosticsGetModeGETApiDevicesZwaveDiagnosticsMode',
      'zWaveDiagnosticsStartInclusionPOSTApiDevicesZwaveDiagnosticsInclusionStart',
      'zWaveDiagnosticsStartExclusionPOSTApiDevicesZwaveDiagnosticsExclusionStart',
      'zWaveDiagnosticsStopModePOSTApiDevicesZwaveDiagnosticsExclusionStop',
      'zWaveDiagnosticsGetAssociationGroupsGETApiDevicesZwaveDiagnosticsNodesNodeIdAssociationGroups',
      'zWaveDiagnosticsSetAssociationGroupPUTApiDevicesZwaveDiagnosticsNodesNodeIdAssociationGroupsGroupId',
      'zWaveDiagnosticsGetParametersGETApiDevicesZwaveDiagnosticsNodesNodeIdParameters',
      'zWaveDiagnosticsSetParameterPUTApiDevicesZwaveDiagnosticsNodesNodeIdParametersParamId'
    ]);
    client.zWaveDiagnosticsGetNodesGETApiDevicesZwaveDiagnosticsNodes.and.returnValue(of([
      { nodeId: 1, status: ZWaveNodeStatusModel.NUMBER_0, isKnown: true, elementType: 'ControllerElement', neighbors: [2] },
      { nodeId: 6, status: ZWaveNodeStatusModel.NUMBER_1, isKnown: false, neighbors: [] }
    ]) as any);
    client.zWaveDiagnosticsGetModeGETApiDevicesZwaveDiagnosticsMode.and.returnValue(
      of({ controllerAvailable: true, mode: ZWaveInclusionModeModel.NUMBER_0 }) as any);

    await TestBed.configureTestingModule({
      declarations: [ZWaveDiagnosticsComponent],
      imports: [FormsModule, TranslateModule.forRoot()],
      providers: [
        { provide: ZWaveDiagnosticsClient, useValue: client },
        { provide: LoggingService, useValue: { error: () => undefined, warn: () => undefined } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ZWaveDiagnosticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  it('marks nodes unknown to the NodeFactory and only offers associations for known ones', () => {
    const rows: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('tbody tr'));

    expect(rows.length).toBe(2);
    expect(rows[0].classList).not.toContain('table-warning');
    expect(rows[0].querySelector('button')).not.toBeNull();
    expect(rows[1].classList).toContain('table-warning');
    expect(rows[1].querySelector('button')).toBeNull();
  });

  it('shows a hint when the controller is unavailable', () => {
    client.zWaveDiagnosticsGetNodesGETApiDevicesZwaveDiagnosticsNodes.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })) as any);

    component.loadNodes();
    fixture.detectChanges();

    expect(component.controllerUnavailable).toBeTrue();
  });

  it('starts inclusion and reflects the active mode', () => {
    client.zWaveDiagnosticsStartInclusionPOSTApiDevicesZwaveDiagnosticsInclusionStart.and.returnValue(of({
      controllerAvailable: true,
      mode: ZWaveInclusionModeModel.NUMBER_1,
      expiresAt: new Date(Date.now() + 60000).toISOString()
    }) as any);

    component.startInclusion();

    expect(component.isInclusion).toBeTrue();
    expect(component.secondsLeft).toBeGreaterThan(0);
  });

  it('reloads nodes when a node was included while watching', () => {
    client.zWaveDiagnosticsGetModeGETApiDevicesZwaveDiagnosticsMode.and.returnValue(of({
      controllerAvailable: true,
      mode: ZWaveInclusionModeModel.NUMBER_1,
      lastEvent: { nodeId: 50, isNodeAdded: true, isKnown: false, timestamp: '2026-11-01T13:00:00Z' }
    }) as any);
    client.zWaveDiagnosticsGetNodesGETApiDevicesZwaveDiagnosticsNodes.calls.reset();

    component.refreshMode();

    expect(client.zWaveDiagnosticsGetNodesGETApiDevicesZwaveDiagnosticsNodes).toHaveBeenCalledTimes(1);
  });

  it('saves a parsed association group', () => {
    client.zWaveDiagnosticsSetAssociationGroupPUTApiDevicesZwaveDiagnosticsNodesNodeIdAssociationGroupsGroupId.and.returnValue(of({}) as any);
    component.associationNodeId = 1;

    component.saveAssociationGroup({ groupId: 1, maxNodesSupported: 5, nodesInput: '1, 5 5', saving: false });

    expect(client.zWaveDiagnosticsSetAssociationGroupPUTApiDevicesZwaveDiagnosticsNodesNodeIdAssociationGroupsGroupId)
      .toHaveBeenCalledWith(1, 1, { nodeIds: [1, 5] });
  });

  it('loads parameters only for known nodes', () => {
    client.zWaveDiagnosticsGetParametersGETApiDevicesZwaveDiagnosticsNodesNodeIdParameters.and.returnValue(of({
      deviceType: 'FibaroWallPlug', hasProvider: true, parameters: [{ id: 1, name: 'Always active', value: 3, size: 1 }]
    }) as any);

    component.openParameters(component.nodes[1]);
    expect(client.zWaveDiagnosticsGetParametersGETApiDevicesZwaveDiagnosticsNodesNodeIdParameters).not.toHaveBeenCalled();

    component.openParameters(component.nodes[0]);
    expect(client.zWaveDiagnosticsGetParametersGETApiDevicesZwaveDiagnosticsNodesNodeIdParameters).toHaveBeenCalledWith(1);
    expect(component.parameters.length).toBe(1);
    expect(component.parameters[0].value).toBe(3);
  });

  it('saves a parameter and shows the value read back from the device', () => {
    client.zWaveDiagnosticsSetParameterPUTApiDevicesZwaveDiagnosticsNodesNodeIdParametersParamId.and.returnValue(
      of({ id: 7, value: 9, size: 1 }) as any);
    component.parameterNodeId = 1;
    const parameter = { id: 7, name: 'x', size: 1, value: 10, saving: false };

    component.saveParameter(parameter);

    expect(client.zWaveDiagnosticsSetParameterPUTApiDevicesZwaveDiagnosticsNodesNodeIdParametersParamId)
      .toHaveBeenCalledWith(1, 7, { value: 10 });
    expect(parameter.value).toBe(9);
  });

  it('does not send parameter values outside a byte', () => {
    component.parameterNodeId = 1;

    component.saveParameter({ id: 7, name: 'x', size: 1, value: 300, saving: false });
    component.saveParameter({ id: 7, name: 'x', size: 1, value: null, saving: false });

    expect(client.zWaveDiagnosticsSetParameterPUTApiDevicesZwaveDiagnosticsNodesNodeIdParametersParamId).not.toHaveBeenCalled();
  });

  it('rejects invalid node ids', () => {
    expect(component.parseNodeIds('1, abc')).toBeNull();
    expect(component.parseNodeIds('0')).toBeNull();
    expect(component.parseNodeIds('233')).toBeNull();
    expect(component.parseNodeIds('')).toEqual([]);
  });
});
