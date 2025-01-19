import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { StatusService, StatusDto, DashboardItemDto, DashboardService, DashboardItemType } from '../services/api-client'; // Import the generated client

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  constructor(public authService: AuthService, private statusService: StatusService, private dashboardService : DashboardService) { }

  currentUserName: string = this.authService.currentUserName;
  currentUserDisplayName: string = this.authService.currentUserDisplayName;
  statusMessage: string = "";
  statusDto: StatusDto|null = null;
  dashboardItems: DashboardItemDto[] = [];

  ITEM_TYPE_DEVICE : DashboardItemType = DashboardItemType.NUMBER_0;
  ITEM_TYPE_SCENE  : DashboardItemType = DashboardItemType.NUMBER_1;
  ITEM_TYPE_ROOM   : DashboardItemType = DashboardItemType.NUMBER_2;
  ITEM_TYPE_PERSON : DashboardItemType = DashboardItemType.NUMBER_3;

  ngOnInit(): void {
    this.onComponentLoad();
  }

  onComponentLoad(): void {
    // Add your logic here that should be executed after the component is loaded
    console.log('HomeComponent loaded');
    this.loadStatus();
    this.loadDashboardItems();
  }


  private loadDashboardItems() {
    this.dashboardService.apiDashboardGet().subscribe({
      next: (items: DashboardItemDto[]) => {
        console.log('Dashboard items:', items);
        this.dashboardItems = items;
      },
      error: (error) => {
        console.error('Error fetching dashboard items:', error);
      }
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

  login(): void {
    this.authService.login();
  }

  logout(): void {
    this.authService.logout();
  }

  isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }
}
