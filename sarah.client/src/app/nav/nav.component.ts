import { Component } from '@angular/core';
import { LanguageService, SupportedLang } from '../services/language.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent {
  navbarOpen = false;

  constructor(public languageService: LanguageService) {}

  toggleNavbar() {
    this.navbarOpen = !this.navbarOpen;
  }

  setLang(lang: SupportedLang): void {
    this.languageService.setLanguage(lang);
  }
}
