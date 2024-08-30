import { Injectable } from '@angular/core';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';


export const authConfig: AuthConfig = {

  // Url des Authorization-Servers
  // see http://pi:8080/realms/sarah/.well-known/openid-configuration
  issuer: 'http://pi:8080/realms/sarah',

  // Url der Angular-Anwendung
  // An diese URL sendet der Authorization-Server den Access Code
  redirectUri: window.location.origin + '/index.html',

  // Name der Angular-Anwendung
  clientId: 'sarah-clientid',

  // Rechte des Benutzers, die die Angular-Anwendung wahrnehmen möchte
  scope: 'openid profile email offline_access sarah-api',

  // Code Flow (PKCE ist standardmäßig bei Nutzung von Code Flow aktiviert)
  responseType: 'code'

}




@Injectable({
  providedIn: 'root'
})
export class AuthService {
  currentUserName: string = "";
  currentUserEmail: string = "";

  constructor(
    private oauthService: OAuthService) 
    {
    }

    
  /**
   * #### Description
   * Inits auth service
   * #### Version
   * since: V1.0.0
   * #### Example
   * 
   * #### Links
   * 
   * 
   * 
   */
  public init(): void {
    this.oauthService.configure(authConfig);
    /*
     * Die Methode loadDiscoveryDocumentAndTryLogin lädt weitere Konfigurationsdaten vom Authorization Server. 
     * Dieses als Discovery bekannte Verfahren ist durch OIDC standardisiert. 
     * Danach prüft diese Methode, ob sich bereits ein Access-Code in der Url befindet. 
     * In diesem Fall versucht sie, den Flow abzuschließen, was im Erfolgsfall zum Erhalt der diskutierten Tokens führt.
     */
    this.oauthService.loadDiscoveryDocumentAndTryLogin();
    /* 
     * Mit setupAutomaticSilentRefresh gibt die Anwendung an, dass die Token automatisch zu erneuern sind. 
     * Danach wird es sehr geradlinig. Die Methode initLoginFlow beginnt mit dem Code-Flow und leitet den 
     * Benutzer zu Authorization-Server um:
     */
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
    let isTokenPresent: boolean;
    isTokenPresent = this.oauthService.hasValidIdToken();
    return isTokenPresent;

    // Die Methode getIdentityClaims liefert Key/Value-Pairs, die den Benutzer beschreiben:

    // const claims = this.oauthService.getIdentityClaims();
    // if (!claims) return null;
    // return claims['given_name'];
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



}
