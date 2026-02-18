/**
 * Sarah Frontend Environment Configuration - Production
 * 
 * This file defines the configuration for the Angular frontend in production mode.
 * 
 * Production Configuration:
 * - All services use HTTPS with proper SSL certificates
 * - Services are accessed via 'pi' hostname (production server)
 * - Keycloak runs on port 8443 (HTTPS)
 * 
 * Microservices API Endpoints:
 * All Sarah microservices are configured here with their base URLs.
 * These URLs are used by the generated OpenAPI clients for HTTP requests.
 * 
 * Authentication:
 * Bearer tokens are automatically added to all requests via AuthInterceptor.
 * The AuthService manages OAuth2/OIDC authentication with Keycloak.
 */
export const environment = {
  production: true,
  apiBaseUrl: 'https://pi:7165',
  bingMapKey: 'FbEWoX5JtiPh8IKcxsSs~2vJ-df0A-rg8A6hMnc4kMA~Att-lde2tVaKCvxFdfh_hhRDuAQ3swla2wldMro8jGMKU9UisIASiabnUfaBs1JC',
  // Keycloak configuration - fixed to port 8443 (standard HTTPS port for production on 'pi' hostname)
  keycloakIssuer: 'https://pi:8443/realms/sarah-realm',
  keycloakClientId: 'sarah-client',
  keycloakRealm: 'sarah-realm',
  // Microservices API endpoints
  // Each service runs on its own port
  api: {
    deviceService: 'https://pi:5001',      // Smart home devices (lamps, sensors, switches)
    personsService: 'https://pi:5002',     // Persons and user profiles
    geofencesService: 'https://pi:5003',   // Geofences and location automation
    roomService: 'https://pi:5004',        // Rooms and device organization
    monitoringService: 'https://pi:5005',  // System monitoring and health checks
    rulesService: 'https://pi:5006',       // Automation rules engine
    speechServer: 'https://pi:5008'        // Voice recognition and text-to-speech
  }
};