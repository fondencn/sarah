import { NgModule, ModuleWithProviders } from '@angular/core';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { ApiConfigService } from './api-config.service';
import { AuthInterceptor } from './auth.interceptor';

// Import all API modules from generated clients
import { ApiModule as DeviceServiceApiModule, BASE_PATH as DEVICE_SERVICE_BASE_PATH } from './api/device-service';
import { ApiModule as PersonsServiceApiModule, BASE_PATH as PERSONS_SERVICE_BASE_PATH } from './api/persons-service';
import { ApiModule as GeofencesServiceApiModule, BASE_PATH as GEOFENCES_SERVICE_BASE_PATH } from './api/geofences-service';
import { ApiModule as RoomServiceApiModule, BASE_PATH as ROOM_SERVICE_BASE_PATH } from './api/room-service';
import { ApiModule as MonitoringServiceApiModule, BASE_PATH as MONITORING_SERVICE_BASE_PATH } from './api/monitoring-service';
import { ApiModule as RulesServiceApiModule, BASE_PATH as RULES_SERVICE_BASE_PATH } from './api/rules-service';
import { ApiModule as SpeechServerApiModule, BASE_PATH as SPEECH_SERVER_BASE_PATH } from './api/speech-server';

import { environment } from '../../environments/environment';

/**
 * Sarah API Module
 * 
 * Centralized module for all OpenAPI-generated microservice clients.
 * 
 * Features:
 * - Imports all generated API modules
 * - Configures base paths from environment
 * - Provides ApiConfigService for bearer token authentication
 * - Includes HTTP interceptor for consistent auth handling
 * - Follows Angular best practices for module organization
 * 
 * Usage:
 * Import this module in your app.module.ts:
 * 
 * ```typescript
 * import { SarahApiModule } from './services/sarah-api.module';
 * 
 * @NgModule({
 *   imports: [
 *     SarahApiModule.forRoot()
 *   ]
 * })
 * export class AppModule { }
 * ```
 */
@NgModule({
  imports: [
    HttpClientModule,
    DeviceServiceApiModule,
    PersonsServiceApiModule,
    GeofencesServiceApiModule,
    RoomServiceApiModule,
    MonitoringServiceApiModule,
    RulesServiceApiModule,
    SpeechServerApiModule
  ],
  exports: [
    DeviceServiceApiModule,
    PersonsServiceApiModule,
    GeofencesServiceApiModule,
    RoomServiceApiModule,
    MonitoringServiceApiModule,
    RulesServiceApiModule,
    SpeechServerApiModule
  ]
})
export class SarahApiModule {
  /**
   * Configure the module with providers for base paths and interceptors
   * Call this method when importing in your root module
   */
  static forRoot(): ModuleWithProviders<SarahApiModule> {
    return {
      ngModule: SarahApiModule,
      providers: [
        ApiConfigService,
        // Configure HTTP interceptor for bearer token authentication
        {
          provide: HTTP_INTERCEPTORS,
          useClass: AuthInterceptor,
          multi: true
        },
        // Configure base paths for all services
        { provide: DEVICE_SERVICE_BASE_PATH, useValue: environment.api.deviceService },
        { provide: PERSONS_SERVICE_BASE_PATH, useValue: environment.api.personsService },
        { provide: GEOFENCES_SERVICE_BASE_PATH, useValue: environment.api.geofencesService },
        { provide: ROOM_SERVICE_BASE_PATH, useValue: environment.api.roomService },
        { provide: MONITORING_SERVICE_BASE_PATH, useValue: environment.api.monitoringService },
        { provide: RULES_SERVICE_BASE_PATH, useValue: environment.api.rulesService },
        { provide: SPEECH_SERVER_BASE_PATH, useValue: environment.api.speechServer }
      ]
    };
  }
}
