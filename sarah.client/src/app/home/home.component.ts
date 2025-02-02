import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { StatusService, StatusDto, DashboardItemDto, DashboardService, DashboardItemTypeDto, DevicesService } from '../services/api-client'; // Import the generated client
import { trigger, transition, style, animate, query, stagger } from '@angular/animations';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
  animations: [
    trigger('popIn', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'scale(0.5)' }),
          stagger(300, [
            animate('0.5s ease-out', style({ opacity: 1, transform: 'scale(1)' }))
          ])
        ], { optional: true })
      ])
    ])
  ]
})
export class HomeComponent implements OnInit {

  constructor(public authService: AuthService, 
    private statusService: StatusService, 
    private dashboardService : DashboardService, 
    private devicesService : DevicesService,
    private cdr: ChangeDetectorRef) { }  

  currentUserName: string = this.authService.currentUserName;
  currentUserDisplayName: string = this.authService.currentUserDisplayName;
  statusMessage: string = "";
  statusDto: StatusDto|null = null;
  dashboardItems: DashboardItemDto[] = [];
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
    setInterval(() => {
      this.updateDashboardItems();
    }, this.UPDATE_MILLISECONDS); // Update every 10 seconds
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

  switchWallplugOff(itemId: number) {
    this.setWallplugState(itemId, false);
  }

  switchWallplugOn(itemId: number) {
    this.setWallplugState(itemId, true);
  }

  setLampColor(itemId: number, eventTarget: EventTarget | null) {
    var element : HTMLInputElement = eventTarget as HTMLInputElement;
    this.setLampColorInternal(itemId, element.value);
  }


  /* ******************** API Calls ******************** */
  private loadDashboardItems() : void {
    this.animateItems = true;
    this.dashboardService.apiDashboardGet().subscribe({
      next: (items: DashboardItemDto[]) => {
        console.log('Dashboard items:', items);
        this.dashboardItems = items;
      },
      error: (error) => {
        console.error('Error fetching dashboard items:', error);
      }
    })
  }



  updateDashboardItems(): void {
    this.animateItems = false;
    this.dashboardService.apiDashboardGet().subscribe(items => {
      this.dashboardItems.forEach((item, index) => {
        const updatedItem = items.find(i => i.itemId === item.itemId && item.itemType === i.itemType);
        if (updatedItem) {
          // Update the properties
          item.description = updatedItem.description;
          item.extendedProperties = updatedItem.extendedProperties;
          item.title = updatedItem.title;


          // Mark for check and manually trigger change detection
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        }
      });
    });
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
    this.devicesService.devicesLampIdColorColorPost(itemId,color).subscribe({
      next: (response: StatusDto) => {
        console.log('setLampColor:', response);
      },
      error: (error) => {
        console.error('Error setting lamp color:', error);
      }
    });
  }

  private setLampBrightness(itemId: number, brightness: number) {
    this.devicesService.devicesLampIdBrightnessBrightnessPost(itemId, brightness).subscribe({
      next: (response: StatusDto) => {
        console.log('setLampBrightness:', response);
      },
      error: (error) => {
        console.error('Error setting lamp brightness:', error);
      }
    });
  }

  private setWallplugState(itemId: number, state: boolean) {
    this.devicesService.devicesWallplugIdIsOnPost(itemId, state).subscribe({
      next: (response: StatusDto) => {
        console.log('setWallplugState:', response);
      },
      error: (error) => {
        console.error('Error setting lamp brightness:', error);
      }
    });
  }

}
