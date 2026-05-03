/// <reference types="jasmine" />

import { NavComponent } from './nav.component';
import { LanguageService } from '../services/language.service';
import { AuthService } from '../services/auth.service';
import { of } from 'rxjs';

describe('NavComponent', () => {
  let component: NavComponent;
  let languageServiceSpy: jasmine.SpyObj<LanguageService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    languageServiceSpy = jasmine.createSpyObj('LanguageService', ['setLanguage'], {
      currentLang$: of('en')
    });
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isLoggedIn', 'logout']);

    component = new NavComponent(languageServiceSpy, authServiceSpy);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should delegate logout to auth service', () => {
    component.logout();

    expect(authServiceSpy.logout).toHaveBeenCalled();
  });

  it('should delegate login state check to auth service', () => {
    authServiceSpy.isLoggedIn.and.returnValue(true);

    const result = component.isLoggedIn();

    expect(result).toBeTrue();
    expect(authServiceSpy.isLoggedIn).toHaveBeenCalled();
  });
});
