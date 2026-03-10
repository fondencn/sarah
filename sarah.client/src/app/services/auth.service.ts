import { Injectable } from '@angular/core';
import { AuthConfig, OAuthEvent, OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';
import { AspireResourceService } from './aspire-resource.service';

// Default auth config - will be updated with discovered endpoint if available
export const authConfig: AuthConfig = {
  issuer: environment.keycloakIssuer ?? 'http://localhost:8080/realms/sarah-realm',
  redirectUri: window.location.origin,
  postLogoutRedirectUri: window.location.origin,
  clientId: environment.keycloakClientId ?? 'sarah-client',
  // No client secret needed - public client as configured in keycloak-realm.json
  scope: 'openid profile email offline_access',
  responseType: 'code',
  showDebugInformation: true,
  strictDiscoveryDocumentValidation: false,
  useHttpBasicAuth: false,
  disableAtHashCheck: true,
  requireHttps: false
};

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Shared across all injections to avoid duplicate init runs from APP_INITIALIZER + component injection.
  private static sharedInitializationPromise: Promise<void> | null = null;

  private initializationPromise: Promise<void>;
  private readonly authInitTimeoutMs = 8000;
  private readonly discoveryAttemptTimeoutMs = 2000;
  private discoveryReady = false;
  private loginInProgress = false;

  constructor(
    private oauthService: OAuthService,
    private aspireResourceService: AspireResourceService
  ) {
    if (!AuthService.sharedInitializationPromise) {
      AuthService.sharedInitializationPromise = this.initializeAuth();
    }
    this.initializationPromise = AuthService.sharedInitializationPromise;
  }

  /**
   * Initialize authentication by discovering Keycloak endpoint (Aspire)
   * Falls back to hardcoded endpoint if Aspire discovery fails
   */
  private async initializeAuth(): Promise<void> {
    try {
      console.log('[AUTH] Initializing authentication service...');

      const shouldTryRuntimeDiscovery = this.shouldAttemptRuntimeDiscovery();
      if (shouldTryRuntimeDiscovery) {
        const realm = environment.keycloakRealm ?? 'sarah-realm';
        const discoveredIssuer = await this.withTimeout(
          this.aspireResourceService.discoverKeycloakIssuer(realm),
          this.discoveryAttemptTimeoutMs
        );

        if (discoveredIssuer) {
          console.log('[AUTH] Using Aspire-discovered issuer:', discoveredIssuer);
          authConfig.issuer = discoveredIssuer;
        } else {
          console.log('[AUTH] Runtime discovery unavailable, using configured issuer:', authConfig.issuer);
        }
      } else {
        console.log('[AUTH] Runtime discovery disabled, using configured endpoint:', authConfig.issuer);
      }

      console.log('[AUTH] Configuring OAuth with issuer:', authConfig.issuer);
      this.oauthService.configure(authConfig);
      this.oauthService.setStorage(localStorage); // Use localStorage to store tokens
      this.subscribeToAuthEvents();

      await this.loadDiscoveryWithTimeout();

      if (this.discoveryReady) {
        this.oauthService.setupAutomaticSilentRefresh();
      }
      if (this.oauthService.hasValidAccessToken()) {
        console.log('[AUTH] Valid access token found.');
      } else {
        console.log('[AUTH] No valid access token found. Waiting for user login action.');
      }
    } catch (err) {
      console.error('[AUTH] Error during initialization:', err);
      // APP_INITIALIZER must resolve so the app can render even if auth bootstrap fails.
    }
  }

  private shouldAttemptRuntimeDiscovery(): boolean {
    if (environment.useAspireRuntimeDiscovery) {
      return true;
    }

    if (typeof window === 'undefined') {
      return false;
    }

    // Auto-enable discovery for local dev where Aspire resource discovery is usually reachable.
    const host = window.location.hostname;
    return host === 'localhost' || host === '127.0.0.1';
  }

  private subscribeToAuthEvents(): void {
    this.oauthService.events.subscribe((event: OAuthEvent) => {
      if (event.type === 'token_received') {
        this.loginInProgress = false;
      }

      // Do not trigger interactive login on token_expires to avoid callback/login loops.
      if (event.type === 'session_terminated' || event.type === 'session_error') {
        void this.login();
      }
    });
  }

  private async loadDiscoveryWithTimeout(): Promise<void> {
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    const timeoutPromise = new Promise<'timeout'>((resolve) => {
      timeoutId = setTimeout(() => {
        console.warn(`[AUTH] Discovery/login timed out after ${this.authInitTimeoutMs}ms; continuing startup.`);
        resolve('timeout');
      }, this.authInitTimeoutMs);
    });

    const discoveryPromise = this.oauthService
      .loadDiscoveryDocumentAndTryLogin()
      .then(() => {
        this.discoveryReady = true;
        console.log('[AUTH] Discovery document loaded');
        return 'discovery' as const;
      })
      .catch((error) => {
        console.warn('[AUTH] Discovery/login failed, continuing without active session:', error);
        return 'failed' as const;
      });

    const result = await Promise.race([discoveryPromise, timeoutPromise]);
    if (result !== 'timeout' && timeoutId) {
      clearTimeout(timeoutId);
    }
  }

  private async withTimeout<T>(promise: Promise<T>, timeoutMs: number): Promise<T | null> {
    let timeoutId: ReturnType<typeof setTimeout> | null = null;
    const timeoutPromise = new Promise<null>((resolve) => {
      timeoutId = setTimeout(() => resolve(null), timeoutMs);
    });

    const result = await Promise.race([promise, timeoutPromise]);
    if (timeoutId) {
      clearTimeout(timeoutId);
    }

    return result as T | null;
  }

  private async ensureDiscoveryReady(): Promise<boolean> {
    if (this.discoveryReady) {
      return true;
    }

    try {
      await this.oauthService.loadDiscoveryDocument();
      this.discoveryReady = true;
      return true;
    } catch (error) {
      console.error('[AUTH] Unable to load discovery document. Is Keycloak reachable at issuer?', authConfig.issuer, error);
      return false;
    }
  }

  /**
   * Wait for auth initialization to complete before proceeding
   */
  async waitForInitialization(): Promise<void> {
    await this.initializationPromise;
  }

    
  /**
   * #### Description
   * gibt an, ob aktuell ein User angemeldet ist
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   * Determines whether logged in is
   */
  public isLoggedIn(): boolean {
    return this.oauthService.hasValidAccessToken();
  }


  /**
   * #### Description
   * meldet alle angemeldeten User über OIDC ab
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   * Logins auth service
   */
  public async login(): Promise<void> {
    // Prevent overlapping redirects when multiple API calls fail at once.
    if (this.loginInProgress) {
      return;
    }

    this.loginInProgress = true;
    console.log('Calling initLoginFlow...');

    const ready = await this.ensureDiscoveryReady();
    if (!ready) {
      this.loginInProgress = false;
      return;
    }

    this.oauthService.initLoginFlow();
  }


  /**
   * #### Description
   * Mit der Methode logOut lässt sich der Benutzer hingegen wieder abmelden:
   * Das bedeutet, dass zum einen die Tokens verworfen werden, aber auch, dass 
   * durch eine Umleitung der Benutzer beim Authorization-Server abgemeldet wird.
   * 
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   * Logouts auth service
   */
  public logout(): void {
    this.oauthService.logOut();
  }

  /**
   * #### Description
   * Gets identity claims
   * 
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   */
  public get identityClaims(): any {
    return this.oauthService.getIdentityClaims();
  }

  /**
   * #### Description
   * Gets access token
   * 
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   * @returns access token 
   */
  public getAccessToken(): string {
    return this.oauthService.getAccessToken();
  }


  /**
   * #### Description
   * Gets current user name
   * 
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   */
  public get currentUserName(): string {
    const claims = this.identityClaims;
    let username: string = '';
    if (claims) {
      username = claims.preferred_username;
    }
    return username;
  }

  /**
   * #### Description
   * Gets current user display name
   * 
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   */
  public get currentUserDisplayName(): string {
    const claims = this.identityClaims;
    let firstname: string = '';
    let lastname: string = '';
    if (claims) {
      firstname = claims.given_name;
      lastname = claims.family_name;
    }
    return `${firstname} ${lastname}`;
  }
}
