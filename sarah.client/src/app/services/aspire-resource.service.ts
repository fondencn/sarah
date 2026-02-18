import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';

/**
 * Service for discovering service endpoints from Aspire's resource service API
 * Aspire exposes dynamic service discovery at https://localhost:15888/resources/{serviceName}/endpoints/{endpointName}
 */
@Injectable({
  providedIn: 'root'
})
export class AspireResourceService {
  // Aspire resource service is typically at localhost:15888
  // But can be overridden by ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL env var
  private readonly ASPIRE_API_URL = this.getAspireApiUrl();

  constructor(private http: HttpClient) {
    console.log('[ASPIRE] Resource discovery service initialized, API URL:', this.ASPIRE_API_URL);
  }

  /**
   * Get the Aspire resource service URL from window location or use default
   */
  private getAspireApiUrl(): string {
    // In development, Aspire usually runs on localhost:15888
    // In production or custom setups, it might be different
    if (typeof window !== 'undefined' && window.location.hostname !== 'localhost') {
      return `https://${window.location.hostname}:15888`;
    }
    return 'https://localhost:15888';
  }

  /**
   * Discover a service endpoint from Aspire
   * @param serviceName The service name as configured in Aspire (e.g., 'keycloak')
   * @param endpointName The endpoint name (e.g., 'https')
   * @returns Promise<string> The full URL (e.g., 'https://localhost:xxxxx')
   */
  async discoverEndpoint(serviceName: string, endpointName: string = 'https'): Promise<string | null> {
    try {
      const url = `${this.ASPIRE_API_URL}/resources/${serviceName}/endpoints/${endpointName}`;
      console.log('[ASPIRE] Discovering endpoint:', url);

      // Aspire uses self-signed certs in development, so we need custom headers
      const response = await firstValueFrom(
        this.http.get<{ address: string; scheme: string; port: number }>(url, {
          // Allow self-signed certificates
          withCredentials: false
        }).pipe(
          catchError(err => {
            console.warn(`[ASPIRE] Failed to discover ${serviceName}/${endpointName}:`, err.status, err.message);
            return of(null);
          })
        )
      );

      if (response && response.address) {
        const fullUrl = `${response.scheme || 'https'}://${response.address}`;
        console.log(`[ASPIRE] ✓ Discovered ${serviceName}:`, fullUrl);
        return fullUrl;
      }

      console.log(`[ASPIRE] No address in response for ${serviceName}`);
      return null;
    } catch (error: any) {
      console.warn('[ASPIRE] Exception discovering endpoint:', error?.message);
      return null;
    }
  }

  /**
   * Discover Keycloak's endpoint and build the OIDC issuer URL
   * @returns Promise<string | null> The Keycloak issuer URL (e.g., 'https://localhost:xxxxx/realms/sarah-realm')
   */
  async discoverKeycloakIssuer(realm: string = 'sarah-realm'): Promise<string | null> {
    try {
      const endpoint = await this.discoverEndpoint('keycloak', 'https');
      if (endpoint) {
        const issuer = `${endpoint}/realms/${realm}`;
        console.log('[ASPIRE] Built Keycloak issuer:', issuer);
        return issuer;
      }
      return null;
    } catch (error: any) {
      console.warn('[ASPIRE] Failed to discover Keycloak issuer:', error?.message);
      return null;
    }
  }
}
