import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { DevicesClient } from '../services/api/device-service/api/devices.service';
import { CreateDashboardItemDto, DashboardItemDto, DashboardItemType, DashboardItemTypeDto, ExtendedPropertyDto, StatusDto } from '../models/api-types';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { StatusRuntimeService } from '../services/status-runtime.service';
import { trigger, transition, style, animate, state } from '@angular/animations';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
  animations: [
    trigger('popIn', [
      state('enabled', style({ opacity: 1, transform: 'scale(1)' })),
      state('disabled', style({ opacity: 1, transform: 'scale(1)' })),
      transition('void => enabled', [
        style({ opacity: 0, transform: 'scale(0.5)' }),
        animate('0.5s ease-out', style({ opacity: 1, transform: 'scale(1)' }))
      ])
    ])
  ]
})
export class HomeComponent implements OnInit {

  constructor(public authService: AuthService, 
    private statusService: StatusRuntimeService, 
    private dashboardService : DashboardRuntimeService, 
    private devicesService : DevicesClient) { }  

  currentUserName: string = this.authService.currentUserName;
  currentUserDisplayName: string = this.authService.currentUserDisplayName;
  statusMessage: string = "";
  statusDto: StatusDto|null = null;
  dashboardItems: DashboardItemViewModel[] = [];
  animateItems: boolean = true; // Flag to control animation

  ITEM_TYPE_DEVICE : DashboardItemTypeDto = DashboardItemTypeDto.NUMBER_0;
  ITEM_TYPE_SCENE  : DashboardItemTypeDto = DashboardItemTypeDto.NUMBER_1;
  ITEM_TYPE_ROOM   : DashboardItemTypeDto = DashboardItemTypeDto.NUMBER_2;
  ITEM_TYPE_PERSON : DashboardItemTypeDto = DashboardItemTypeDto.NUMBER_3;
  
  UPDATE_MILLISECONDS : number = 3000;

  ngOnInit(): void {
    this.onComponentLoad();
    this.startDashboardUpdateTimer();
  }

  onComponentLoad(): void {
    // Add your logic here that should be executed after the component is loaded
    console.log('HomeComponent loaded');
    this.loadStatus();
    this.loadDashboardItems();
  }


  startDashboardUpdateTimer(): void {
    // Only start the timer if user is logged in
    if (this.isLoggedIn()) {
      setInterval(() => {
        this.updateDashboardItems();
      }, this.UPDATE_MILLISECONDS); // Update every 3 seconds
    }
  }


  login(): void {
    this.authService.login();
  }

  logout(): void {
    this.authService.logout();
  }

  isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  switchLampOff(itemId: number) {
    this.setLampBrightness(itemId, 0);
  }

  switchLampOn(itemId: number) {
    this.setLampBrightness(itemId, 100);
  }

  switchWallplugOn(itemId: number) {
    this.setWallplugState(itemId, true);
  }

  switchWallplugOff(itemId: number) {
    this.setWallplugState(itemId, false);
  }

  toggleWallplug(itemId: number | undefined, eventTarget: EventTarget|null) {
    var element: HTMLInputElement = eventTarget as HTMLInputElement;
    const isChecked: boolean = element.checked;
    this.setWallplugState(itemId as number, isChecked);
  }

  toggleLamp(itemId: number | undefined, eventTarget: EventTarget|null) {
    var element: HTMLInputElement = eventTarget as HTMLInputElement;
    const isChecked: boolean = element.checked;
    if(isChecked) {
      this.switchLampOn(itemId as number);
    } else {
      this.switchLampOff(itemId as number);
    } 
  }

  setLampColor(itemId: number, eventTarget: EventTarget | null) {
    var element : HTMLInputElement = eventTarget as HTMLInputElement;
    this.setLampColorInternal(itemId, element.value);
  }




  /* ******************** API Calls ******************** */
  private loadDashboardItems(): void {
    this.animateItems = true;
    this.dashboardService.apiDashboardGet().subscribe({
      next: (items: DashboardItemDto[]) => {
        this.dashboardItems = items.map(item => new DashboardItemViewModel(item));
      },
      error: (error) => {
        console.error('Error fetching dashboard items:', error);
      }
    });
  }

  removeDashboardItem(item: DashboardItemViewModel): void {
    this.dashboardService.apiDashboardItemIdItemTypeDelete(item.itemId, item.itemType as number as DashboardItemType).subscribe({
      next: () => {
        this.dashboardItems = this.dashboardItems.filter(i => !(i.itemId === item.itemId && i.itemType === item.itemType));
      },
      error: (error) => {
        console.error('Error removing dashboard item:', error);
      }
    });
  }



  updateDashboardItems(): void {
    this.animateItems = false;
    this.dashboardService.apiDashboardGet().subscribe({
      next: (items: DashboardItemDto[]) => {
        this.dashboardItems = items.map(item => new DashboardItemViewModel(item));
      },
      error: (error) => {
        console.error('Error updating dashboard items:', error);
      }
    });
  }

  trackByDashboardItem(index: number, item: DashboardItemViewModel): string {
    return `${item.itemType}-${item.itemId}`;
  }


  private loadStatus() {
    this.statusMessage = "Component has been loaded.";
    this.statusDto = null;

    // Call the /status endpoint using the generated client
    this.statusService.statusGet().subscribe({
      next: (response: StatusDto) => {
        console.log('Status:', response);
        this.statusDto = response;
        this.statusMessage = "Status fetched successfully.";
      },
      error: (error) => {
        console.error('Error fetching status:', error);
        this.statusMessage = "Error fetching status.";
        this.statusDto = null;
      }
    });
  }



  private setLampColorInternal(itemId: number, color: string) {
    this.devicesService.devicesSetLampColorPOSTApiDevicesLampIdColorColor(itemId, color).subscribe({
      next: (response: StatusDto) => {
        console.log('setLampColor:', response);
      },
      error: (error) => {
        console.error('Error setting lamp color:', error);
      }
    });
  }

  private setLampBrightness(itemId: number, brightness: number) {
    this.devicesService.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness(itemId, brightness).subscribe({
      next: (response: StatusDto) => {
        console.log('setLampBrightness:', response);
      },
      error: (error) => {
        console.error('Error setting lamp brightness:', error);
      }
    });
  }

  private setWallplugState(itemId: number, state: boolean) {
    this.devicesService.devicesSetWallplugStateByDeviceIdPOSTApiDevicesWallplugIdStateIsOn(itemId, state).subscribe({
      next: (response: StatusDto) => {
        console.log('setWallplugState:', response);
      },
      error: (error) => {
        console.error('Error setting lamp brightness:', error);
      }
    });
  }
}

export class DashboardItemViewModel {
  constructor(public item: DashboardItemDto) {}

  get lampColor(): string | null | undefined {
    return this.item.extendedProperties?.find(x => x.key === 'Color')?.value;
  }

  get isOn(): boolean | null | undefined {
    return this.item.extendedProperties?.find(x => x.key === 'IsOn')?.value === 'True';
  }

  get powerConsumption(): number | null | undefined {
    return Number(this.item.extendedProperties?.find(x => x.key === 'Meter_W')?.value ?? "0");
  }

  get itemId(): number {
    return this.item.itemId as number;
  }

  set itemId(value: number) {
    this.item.itemId = value;
  }

  get itemType(): DashboardItemTypeDto {
    return this.item.itemType as DashboardItemTypeDto;
  }

  set itemType(value: DashboardItemTypeDto) {
    this.item.itemType = value;
  }

  get subType(): string {
    return this.item.subtype as string;
  }

  set subType(value: string) {
    this.item.subtype = value;
  }

  get title(): string {
    return this.item.title as string;
  }

  set title(value: string) {
    this.item.title = value;
  }

  get description(): string {
    return this.item.description as string;
  }

  set description(value: string) {
    this.item.description = value;
  }

  get extendedProperties(): ExtendedPropertyDto[] {
    return this.item.extendedProperties as ExtendedPropertyDto[];
  }

  set extendedProperties(value: any[]) {
    this.item.extendedProperties = value;
  }
}
