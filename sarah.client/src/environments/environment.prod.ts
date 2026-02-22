/**
 * Sarah Frontend Environment Configuration - Production
 * 
 * This file defines the configuration for the Angular frontend in production mode.
 * 
 * Production Configuration:
 * - All services use HTTP
 * - Services are accessed via 'pi' hostname (production server)
 * - Keycloak runs on port 8080 (HTTP)
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
  apiBaseUrl: 'http://pi:7165',
  bingMapKey: 'FbEWoX5JtiPh8IKcxsSs~2vJ-df0A-rg8A6hMnc4kMA~Att-lde2tVaKCvxFdfh_hhRDuAQ3swla2wldMro8jGMKU9UisIASiabnUfaBs1JC',
  // Keycloak configuration - fixed to port 8080 (HTTP port for production on 'pi' hostname)
  keycloakIssuer: 'http://pi:8080/realms/sarah-realm',
  keycloakClientId: 'sarah-client',
  keycloakRealm: 'sarah-realm',
  // Microservices API endpoints
  // Each service runs on its own port
  api: {
    dashboardService: 'http://pi:5007',       // Dashboard items and status
    deviceService: 'http://pi:5001',      // Smart home devices (lamps, sensors, switches)
    personsService: 'http://pi:5002',     // Persons and user profiles
    geofencesService: 'http://pi:5003',   // Geofences and location automation
    roomService: 'http://pi:5004',        // Rooms and device organization
    monitoringService: 'http://pi:5005',  // System monitoring and health checks
    rulesService: 'http://pi:5006',       // Automation rules engine
    speechServer: 'http://pi:5008'        // Voice recognition and text-to-speech
  }
};