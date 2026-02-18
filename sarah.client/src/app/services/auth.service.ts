import { Injectable } from '@angular/core';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';
import { AspireResourceService } from './aspire-resource.service';

// Default auth config - will be updated with discovered endpoint if available
export const authConfig: AuthConfig = {
  issuer: environment.keycloakIssuer ?? 'https://localhost:25443/realms/sarah-realm',
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
  requireHttps: true
};

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private initializationPromise: Promise<void>;

  constructor(
    private oauthService: OAuthService,
    private aspireResourceService: AspireResourceService
  ) {
    this.initializationPromise = this.initializeAuth();
  }

  /**
   * Initialize authentication by discovering Keycloak endpoint (Aspire)
   * Falls back to hardcoded endpoint if Aspire discovery fails
   */
  private async initializeAuth(): Promise<void> {
    try {
      console.log('[AUTH] Initializing authentication service...');
      
      // Try to discover Keycloak endpoint from Aspire
      const discoveredIssuer = await this.aspireResourceService.discoverKeycloakIssuer();
      
      if (discoveredIssuer) {
        console.log('[AUTH] ✓ Using Aspire-discovered endpoint:', discoveredIssuer);
        authConfig.issuer = discoveredIssuer;
      } else {
        console.log('[AUTH] Using fallback hardcoded endpoint:', authConfig.issuer);
      }

      console.log('[AUTH] Configuring OAuth with issuer:', authConfig.issuer);
      this.oauthService.configure(authConfig);
      this.oauthService.setStorage(localStorage); // Use localStorage to store tokens
      
      await this.oauthService.loadDiscoveryDocumentAndTryLogin();
      console.log('[AUTH] Discovery document loaded');
      
      if (!this.oauthService.hasValidAccessToken()) {
        console.log('[AUTH] No valid access token found');
        this.oauthService.initLoginFlow();
      }
      
      this.oauthService.setupAutomaticSilentRefresh();
    } catch (err) {
      console.error('[AUTH] Error during initialization:', err);
      throw err;
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
  public login(): void {
    console.log("Calling initLoginFlow...");
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
