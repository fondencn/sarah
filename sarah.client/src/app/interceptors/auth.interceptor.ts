import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  // Keep interceptor independent from AuthService to avoid HTTP_INTERCEPTORS DI cycles.
  private microserviceUrls: string[] = [];
  private loginRedirectInProgress = false;

  constructor(private oauthService: OAuthService) {
    this.microserviceUrls = [
      environment.api.deviceService,
      environment.api.personsService,
      environment.api.geofencesService,
      environment.api.roomService,
      environment.api.monitoringService,
      environment.api.rulesService,
      environment.api.speechServer,
      environment.api.dashboardService,
      environment.api.adminService
    ];
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const isMicroserviceRequest = this.microserviceUrls.some((url) => req.url.startsWith(url));
    if (isMicroserviceRequest && this.oauthService.hasValidAccessToken()) {
      const token = this.oauthService.getAccessToken();
      if (token) {
        req = req.clone({
          headers: req.headers.set('Authorization', `Bearer ${token}`)
        });
      }
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (
          error.status === 401 &&
          isMicroserviceRequest &&
          !this.oauthService.hasValidAccessToken() &&
          !this.isOidcCallbackInProgress()
        ) {
          if (!this.loginRedirectInProgress) {
            // Guard against repeated redirects from concurrent failing requests.
            this.loginRedirectInProgress = true;
            this.oauthService.initLoginFlow();
          }
        }
        return throwError(() => error);
      })
    );
  }

  private isOidcCallbackInProgress(): boolean {
    if (typeof window === 'undefined') {
      return false;
    }

    // During the OIDC code callback we must not start another login flow.
    const params = new URLSearchParams(window.location.search);
    return params.has('code') || params.has('state') || params.has('error');
  }
}