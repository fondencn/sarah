import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { OAuthService } from 'angular-oauth2-oidc';
import { AspireResourceService } from './aspire-resource.service';
import { LoggingService } from './logging.service';

describe('AuthService', () => {
  let service: AuthService;
  let oauthServiceSpy: jasmine.SpyObj<OAuthService>;

  beforeEach(() => {
    // Reset shared initialization so each test gets a fresh instance
    (AuthService as any).sharedInitializationPromise = null;

    oauthServiceSpy = jasmine.createSpyObj(
      'OAuthService',
      [
        'configure',
        'setupAutomaticSilentRefresh',
        'getIdentityClaims',
        'getAccessToken',
        'hasValidAccessToken',
        'setStorage',
        'initLoginFlow',
        'logOut',
        'loadDiscoveryDocumentAndTryLogin'
      ],
      {
        events: { subscribe: () => ({}) } as any
      }
    );
    oauthServiceSpy.loadDiscoveryDocumentAndTryLogin.and.returnValue(Promise.resolve(true));

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: OAuthService, useValue: oauthServiceSpy },
        { provide: AspireResourceService, useValue: { getResourceUrl: () => Promise.resolve(null) } },
        { provide: LoggingService, useValue: { debug: () => {}, info: () => {}, warn: () => {}, error: () => {} } }
      ]
    });
    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('currentUserDisplayName', () => {
    it('returns empty string when not authenticated (no claims)', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue(null as any);
      expect(service.currentUserDisplayName).toBe('');
    });

    it('returns full name when both given_name and family_name are present', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ given_name: 'John', family_name: 'Doe', preferred_username: 'jdoe' });
      expect(service.currentUserDisplayName).toBe('John Doe');
    });

    it('falls back to preferred_username when given_name and family_name are undefined', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ given_name: undefined, family_name: undefined, preferred_username: 'jdoe' });
      expect(service.currentUserDisplayName).toBe('jdoe');
    });

    it('falls back to preferred_username when given_name and family_name are missing from claims', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ preferred_username: 'jdoe' });
      expect(service.currentUserDisplayName).toBe('jdoe');
    });

    it('uses only given_name when family_name is undefined', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ given_name: 'John', family_name: undefined, preferred_username: 'jdoe' });
      expect(service.currentUserDisplayName).toBe('John');
    });

    it('uses only family_name when given_name is undefined', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ given_name: undefined, family_name: 'Doe', preferred_username: 'jdoe' });
      expect(service.currentUserDisplayName).toBe('Doe');
    });
  });

  describe('currentUserName', () => {
    it('returns empty string when not authenticated (no claims)', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue(null as any);
      expect(service.currentUserName).toBe('');
    });

    it('returns preferred_username when present', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ preferred_username: 'jdoe' });
      expect(service.currentUserName).toBe('jdoe');
    });

    it('returns empty string when preferred_username is undefined', () => {
      oauthServiceSpy.getIdentityClaims.and.returnValue({ preferred_username: undefined });
      expect(service.currentUserName).toBe('');
    });
  });
});
