import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

/**
 * Authentication Interceptor
 * 
 * Automatically adds bearer token to all HTTP requests going to Sarah microservices.
 * This is an Angular best practice for handling authentication consistently.
 * 
 * Features:
 * - Adds Authorization header with bearer token
 * - Only affects requests to configured microservices
 * - Handles 401 errors by redirecting to login
 * - Type-safe and testable
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  private microserviceUrls: string[] = [];

  constructor(private authService: AuthService) {
    // Build list of microservice URLs from environment
    this.microserviceUrls = [
      environment.api.deviceService,
      environment.api.personsService,
      environment.api.geofencesService,
      environment.api.roomService,
      environment.api.monitoringService,
      environment.api.rulesService,
      environment.api.speechServer
    ];
  }

  /**
   * Intercept HTTP requests and add authentication
   */
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    
    // Check if this request is for one of our microservices
    const isMicroserviceRequest = this.microserviceUrls.some(url => 
      request.url.startsWith(url)
    );

    // Add bearer token if:
    // 1. Request is for a microservice
    // 2. User is logged in
    if (isMicroserviceRequest && this.authService.isLoggedIn()) {
      const token = this.authService.getAccessToken();
      
      if (token) {
        request = request.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        });
      }
    }

    // Handle the request and catch authentication errors
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Unauthorized - token might be expired or invalid
          // In production, this should use a proper logging service
          if (typeof console !== 'undefined') {
            console.warn('Authentication error (401) - redirecting to login');
          }
          this.authService.login();
        }
        return throwError(() => error);
      })
    );
  }
}
