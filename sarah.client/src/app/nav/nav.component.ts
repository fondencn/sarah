import { Component } from '@angular/core';
import { LanguageService, SupportedLang } from '../services/language.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent {
  navbarOpen = false;

  constructor(
    public languageService: LanguageService,
    private authService: AuthService
  ) {}

  toggleNavbar() {
    this.navbarOpen = !this.navbarOpen;
  }

  setLang(lang: SupportedLang): void {
    this.languageService.setLanguage(lang);
  }

  isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  logout(): void {
    this.authService.logout();
  }
}
