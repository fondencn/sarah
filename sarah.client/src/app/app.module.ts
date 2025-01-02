import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http'; // Import HttpClientModule
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { OAuthModule } from 'angular-oauth2-oidc';
import { FormsModule } from '@angular/forms';
import { ApiModule, Configuration } from './services/api-client'; // Import the generated client
import { NavComponent } from './nav/nav.component';
import { DevicesComponent } from './devices/devices.component';
import { AdminComponent } from './admin/admin.component';
import { environment } from '../environments/environment'; // Import environment configuration
import { AuthInterceptor } from './interceptors/auth.interceptor'; // Import the interceptor

@NgModule({ declarations: [
        AppComponent,
        HomeComponent,
        NavComponent,
        DevicesComponent,
        AdminComponent
    ],
    bootstrap: [AppComponent], 
    imports: [
        BrowserModule,
        AppRoutingModule, 
        OAuthModule.forRoot(
            {
                resourceServer: 
                {
                    sendAccessToken: true,
                    allowedUrls: ['https://localhost', 'https://pi']
                }
            }), 
        FormsModule,
        HttpClientModule, 
        ApiModule.forRoot(() => new Configuration({ basePath: environment.apiBaseUrl })) // Use environment configuration
    ], 
    providers: [
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true } // Provide the interceptor
    ] })
export class AppModule { }
