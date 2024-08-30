import { Component, OnDestroy, OnInit } from '@angular/core';
import { UiService } from './services/ui.service';
import { Router } from '@angular/router';
import { AuthService } from './services/auth.service';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  showMenu = false;
  darkModeActive: boolean = false;
  loggedIn = false;
  sub1: any;
  username: string;
  email: string;

  constructor(
    public ui: UiService, 
    public router: Router, 
    private authService: AuthService
  ) 
  {
    this.authService.init();
    this.username = this.authService.currentUserName;
    this.email = this.authService.currentUserEmail;
  }


  ngOnInit() {
    this.sub1 = this.ui.darkModeState.subscribe((value: any) => {
      this.darkModeActive = value;
    });

  }

  toggleMenu() {
    this.showMenu = !this.showMenu;
  }

  modeToggleSwitch() {
    this.ui.darkModeState.next(!this.darkModeActive);
  }

  ngOnDestroy() {
    this.sub1.unsubscribe();
  }

  logout() {
    this.toggleMenu();
    this.authService.logout();
  }

}
