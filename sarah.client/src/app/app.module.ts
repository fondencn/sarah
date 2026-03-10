import { NgModule, APP_INITIALIZER } from '@angular/core';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http'; // Import HttpClientModule
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { OAuthModule } from 'angular-oauth2-oidc';
import { FormsModule } from '@angular/forms';
import { DevicesClient } from './services/api/device-service/api/api';
import { PersonsClient } from './services/api/persons-service/api/api';
import { RoomsClient } from './services/api/room-service/api/api';
import { DashboardRuntimeService } from './services/dashboard-runtime.service';
import { LocationRuntimeService } from './services/location-runtime.service';
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
import { BingMapComponent } from './shared/bing-map/bing-map.component';

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
        BingMapComponent
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
        BrowserAnimationsModule
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
        { provide: DashboardRuntimeService, useFactory: (http: HttpClient) => new DashboardRuntimeService(http), deps: [HttpClient] },
        { provide: LocationRuntimeService, useFactory: (http: HttpClient) => new LocationRuntimeService(http), deps: [HttpClient] },
        { provide: StatusRuntimeService, useFactory: (http: HttpClient) => new StatusRuntimeService(http), deps: [HttpClient] }
    ] })
export class AppModule { }
