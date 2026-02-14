/**
 * Sarah Frontend Environment Configuration
 * 
 * This file defines the configuration for the Angular frontend in development mode.
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
  production: false,
  bingMapKey: 'FbEWoX5JtiPh8IKcxsSs~2vJ-df0A-rg8A6hMnc4kMA~Att-lde2tVaKCvxFdfh_hhRDuAQ3swla2wldMro8jGMKU9UisIASiabnUfaBs1JC',
  // Keycloak configuration - can be overridden by environment variables from Aspire
  keycloakIssuer: 'https://localhost:8443/realms/sarah-realm',
  keycloakClientId: 'sarah-client',
  keycloakRealm: 'sarah-realm',
  // Microservices API endpoints
  // Each service runs on its own port for development
  api: {
    deviceService: 'https://localhost:5001',      // Smart home devices (lamps, sensors, switches)
    personsService: 'https://localhost:5002',     // Persons and user profiles
    geofencesService: 'https://localhost:5003',   // Geofences and location automation
    roomService: 'https://localhost:5004',        // Rooms and device organization
    monitoringService: 'https://localhost:5005',  // System monitoring and health checks
    rulesService: 'https://localhost:5006',       // Automation rules engine
    speechServer: 'https://localhost:5008'        // Voice recognition and text-to-speech
  }
};