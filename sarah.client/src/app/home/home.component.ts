import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { StatusService, StatusDto } from '../services/api-client'; // Import the generated client

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  constructor(public authService: AuthService, private statusService: StatusService) { }

  currentUserName: string = this.authService.currentUserName;
  currentUserDisplayName: string = this.authService.currentUserDisplayName;
  statusMessage: string = "";

  ngOnInit(): void {
    this.onComponentLoad();
  }

  onComponentLoad(): void {
    // Add your logic here that should be executed after the component is loaded
    console.log('HomeComponent loaded');
    this.statusMessage = "Component has been loaded.";

    // Call the /status endpoint using the generated client
    this.statusService.statusGet().subscribe({
      next: (response: StatusDto) => {
        console.log('Status:', response);
        this.statusMessage = `Hostname: ${response.hostname}, Port: ${response.port}, Authenticated: ${response.isAuthenticated}`;
      },
      error: (error) => {
        console.error('Error fetching status:', error);
        this.statusMessage = "Error fetching status.";
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
