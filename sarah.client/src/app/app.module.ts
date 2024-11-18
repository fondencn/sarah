import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { OAuthModule } from 'angular-oauth2-oidc';
import { FormsModule } from '@angular/forms';
import { NavComponent } from './nav/nav.component';
import { DevicesComponent } from './devices/devices.component';
import { AdminComponent } from './admin/admin.component';


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
                    allowedUrls: ['http://localhost', 'http://pi']
                }
            }), 
        FormsModule
    ], 
    providers: [provideHttpClient(withInterceptorsFromDi())] })
export class AppModule { }
