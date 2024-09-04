import { Injectable } from '@angular/core';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  issuer: 'http://pi:8080/realms/sarah',
  redirectUri: window.location.origin + '/index.html',
  clientId: 'sarah-client',
  scope: 'openid profile email offline_access sarah-api',
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

  constructor(
    private oauthService: OAuthService) 
    {
      this.oauthService.configure(authConfig);
      this.oauthService.loadDiscoveryDocumentAndTryLogin().then(() => {
        console.log('Discovery document loaded');
      }).catch(err => {
        console.error('Error loading discovery document', err);
      });
      this.oauthService.setupAutomaticSilentRefresh();
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

  public get identityClaims(): any {
    return this.oauthService.getIdentityClaims();
  }

  public get currentUserName(): string {
    const claims = this.identityClaims;
    let username = '';
    if (claims) {
      username = claims.preferred_username;
    }
    return username;
  }
}
