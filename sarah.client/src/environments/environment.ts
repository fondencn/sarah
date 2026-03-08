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
  // Keycloak configuration - fixed to port 8080 for local development (Aspire managed endpoint)
  keycloakIssuer: 'http://localhost:8080/realms/sarah-realm',
  keycloakClientId: 'sarah-client',
  keycloakRealm: 'sarah-realm',
  // Browser calls to Aspire's resource endpoint often fail due CORS/auth redirects.
  // Keep this off by default and rely on build-time env substitution in replace-env-vars.js.
  useAspireRuntimeDiscovery: false,
  // Microservices API endpoints
  // Each service runs on its own port for development
  api: {
    dashboardService: 'http://localhost:5007',   // Dashboard items and status
    deviceService: 'http://localhost:5001',      // Smart home devices (lamps, sensors, switches)
    personsService: 'http://localhost:5002',     // Persons and user profiles
    geofencesService: 'http://localhost:5003',   // Geofences and location automation
    roomService: 'http://localhost:5004',        // Rooms and device organization
    monitoringService: 'http://localhost:5005',  // System monitoring and health checks
    rulesService: 'http://localhost:5006',       // Automation rules engine
    speechServer: 'http://localhost:5008'        // Voice recognition and text-to-speech
  }
};