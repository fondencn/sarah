import { NgModule, APP_INITIALIZER, isDevMode } from '@angular/core';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http'; // Import HttpClientModule
import { BrowserModule } from '@angular/platform-browser';
import { ServiceWorkerModule } from '@angular/service-worker';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { DragDropModule } from '@angular/cdk/drag-drop';

export function HttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { OAuthModule } from 'angular-oauth2-oidc';
import { FormsModule } from '@angular/forms';
import { DevicesClient } from './services/api/device-service/api/devices.service';
import { PersonsClient } from './services/api/persons-service/api/persons.service';
import { RoomsClient } from './services/api/room-service/api/rooms.service';
import { RulesClient } from './services/api/rules-service/api/rules.service';
import { GeofencesClient } from './services/api/geofences-service/api/geofences.service';
import { DashboardRuntimeService } from './services/dashboard-runtime.service';
import { StatusRuntimeService } from './services/status-runtime.service';
import { NavComponent } from './nav/nav.component';
import { DevicesComponent } from './devices/devices.component';
import { AdminComponent } from './admin/admin.component';
import { environment } from '../environments/environment'; // Import environment configuration
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { AuthService } from './services/auth.service';
import { EditDeviceModalComponent } from './devices/edit-device-modal/edit-device-modal.component'; 
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { PersonsComponent } from './persons/persons.component';
import { RoomsComponent } from './rooms/rooms.component';
import { EditRoomModalComponent } from './rooms/edit-room-modal/edit-room-modal.component';
import { EditPersonModalComponent } from './persons/edit-person-modal/edit-person-modal.component';
import { HomedetailsComponent } from './home/details/homedetails.component';
import { PersondetailsComponent } from './home/details/person/persondetails/persondetails.component';
import { RoomdetailsComponent } from './home/details/room/roomdetails/roomdetails.component';
import { DevicedetailsComponent } from './home/details/device/devicedetails/devicedetails.component';
import { OsmMapComponent } from './shared/osm-map/osm-map.component';
import { AlarmsComponent } from './alarms/alarms.component';
import { AlarmEditModalComponent } from './alarms/alarm-edit-modal/alarm-edit-modal.component';
import { FullCalendarModule } from '@fullcalendar/angular';
import { PersonMapModalComponent } from './home/person-map-modal/person-map-modal.component';
import { PinToDashboardButtonComponent } from './shared/pin-to-dashboard-button/pin-to-dashboard-button.component';
import { ToastContainerComponent } from './shared/toast/toast-container.component';
import { ThermostatDialComponent } from './home/thermostat-dial/thermostat-dial.component';
import { PromptRulesManagerComponent } from './admin/prompt-rules-manager/prompt-rules-manager.component';
import { DeviceControlModalComponent } from './devices/device-control-modal/device-control-modal.component';
import { KernelConversationPanelComponent } from './admin/kernel-conversation-panel/kernel-conversation-panel.component';

@NgModule({ declarations: [
        AppComponent,
        HomeComponent,
        NavComponent,
        DevicesComponent,
        AdminComponent,
        EditDeviceModalComponent,
        PersonsComponent,
        RoomsComponent,
        EditRoomModalComponent,
        EditPersonModalComponent,
        HomedetailsComponent,
        PersondetailsComponent,
        RoomdetailsComponent,
        DevicedetailsComponent,
        OsmMapComponent,
        AlarmsComponent,
	AlarmEditModalComponent,
        PersonMapModalComponent,
        PinToDashboardButtonComponent,
        ToastContainerComponent,
        ThermostatDialComponent,
        PromptRulesManagerComponent,
        KernelConversationPanelComponent,
        DeviceControlModalComponent
    ],
    bootstrap: [AppComponent], 
    imports: [
        BrowserModule,
        AppRoutingModule, 
        CommonModule,
        ReactiveFormsModule,
        OAuthModule.forRoot(
            {
                resourceServer: 
                {
                    sendAccessToken: true,
                    allowedUrls: ['http://localhost', 'http://pi']
                }
            }), 
        FormsModule,
        HttpClientModule, 
        BrowserAnimationsModule,
        DragDropModule,
        FullCalendarModule,
        TranslateModule.forRoot({
            defaultLanguage: 'de',
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpClient]
            }
        }),
        ServiceWorkerModule.register('ngsw-worker.js', {
            enabled: !isDevMode(),
            // Register the ServiceWorker as soon as the application is stable
            // or after 30 seconds (whichever comes first).
            registrationStrategy: 'registerWhenStable:30000'
        })
    ], 
    providers: [
        {
            provide: APP_INITIALIZER,
            useFactory: (authService: AuthService) => () => authService.waitForInitialization(),
            deps: [AuthService],
            multi: true
        },
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }, // Provide the interceptor
        // Configure base paths per service — each generated service is providedIn:'root' but
        // shares a single BASE_PATH token, so we override with per-service factory providers.
        { provide: DevicesClient, useFactory: (http: HttpClient) => new DevicesClient(http, environment.api.deviceService, undefined!), deps: [HttpClient] },
        { provide: PersonsClient, useFactory: (http: HttpClient) => new PersonsClient(http, environment.api.personsService, undefined!), deps: [HttpClient] },
        { provide: RoomsClient, useFactory: (http: HttpClient) => new RoomsClient(http, environment.api.roomService, undefined!), deps: [HttpClient] },
        { provide: RulesClient, useFactory: (http: HttpClient) => new RulesClient(http, environment.api.rulesService, undefined!), deps: [HttpClient] },
        { provide: GeofencesClient, useFactory: (http: HttpClient) => new GeofencesClient(http, environment.api.geofencesService, undefined!), deps: [HttpClient] },
        { provide: DashboardRuntimeService, useFactory: (http: HttpClient) => new DashboardRuntimeService(http), deps: [HttpClient] },
        { provide: StatusRuntimeService, useFactory: (http: HttpClient) => new StatusRuntimeService(http), deps: [HttpClient] }
    ] })
export class AppModule { }
