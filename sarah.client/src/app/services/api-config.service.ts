import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { Configuration as DeviceServiceConfig } from './api/device-service';
import { Configuration as PersonsServiceConfig } from './api/persons-service';
import { Configuration as GeofencesServiceConfig } from './api/geofences-service';
import { Configuration as RoomServiceConfig } from './api/room-service';
import { Configuration as MonitoringServiceConfig } from './api/monitoring-service';
import { Configuration as RulesServiceConfig } from './api/rules-service';
import { Configuration as SpeechServerConfig } from './api/speech-server';
import { environment } from '../../environments/environment';

/**
 * API Configuration Service
 * 
 * Provides centralized configuration for all OpenAPI-generated clients.
 * Handles bearer token authentication consistently across all microservices.
 * 
 * This service follows Angular best practices:
 * - Singleton service (providedIn: 'root')
 * - Centralized configuration management
 * - Consistent auth handling
 * - Type-safe configuration objects
 */
@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {

  constructor(private authService: AuthService) { }

  /**
   * Creates a configuration object for OpenAPI clients with bearer token authentication
   * @param basePath The base URL of the microservice
   * @returns Configuration object with auth token provider
   */
  private createConfig(basePath: string): any {
    return {
      basePath: basePath,
      credentials: {
        bearer: () => this.authService.getAccessToken()
      }
    };
  }

  /**
   * Get configuration for Device Service API client
   * Manages smart home devices (lamps, sensors, switches)
   */
  get deviceServiceConfig(): DeviceServiceConfig {
    return new DeviceServiceConfig(
      this.createConfig(environment.api.deviceService)
    );
  }

  /**
   * Get configuration for Persons Service API client
   * Manages persons and user profiles
   */
  get personsServiceConfig(): PersonsServiceConfig {
    return new PersonsServiceConfig(
      this.createConfig(environment.api.personsService)
    );
  }

  /**
   * Get configuration for Geofences Service API client
   * Manages geofences and location-based automation
   */
  get geofencesServiceConfig(): GeofencesServiceConfig {
    return new GeofencesServiceConfig(
      this.createConfig(environment.api.geofencesService)
    );
  }

  /**
   * Get configuration for Room Service API client
   * Manages rooms and device organization
   */
  get roomServiceConfig(): RoomServiceConfig {
    return new RoomServiceConfig(
      this.createConfig(environment.api.roomService)
    );
  }

  /**
   * Get configuration for Monitoring Service API client
   * System monitoring and health checks
   */
  get monitoringServiceConfig(): MonitoringServiceConfig {
    return new MonitoringServiceConfig(
      this.createConfig(environment.api.monitoringService)
    );
  }

  /**
   * Get configuration for Rules Service API client
   * Automation rules engine
   */
  get rulesServiceConfig(): RulesServiceConfig {
    return new RulesServiceConfig(
      this.createConfig(environment.api.rulesService)
    );
  }

  /**
   * Get configuration for Speech Server API client
   * Voice recognition and text-to-speech
   */
  get speechServerConfig(): SpeechServerConfig {
    return new SpeechServerConfig(
      this.createConfig(environment.api.speechServer)
    );
  }
}
