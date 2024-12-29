import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  constructor(public authService: AuthService) {}

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
