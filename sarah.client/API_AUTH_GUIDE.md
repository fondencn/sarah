# Sarah OpenAPI Client Authentication Guide

This guide explains how bearer token authentication is handled consistently across all Sarah microservice clients.

## Architecture Overview

The Sarah frontend uses a centralized authentication approach:

```
┌─────────────────────────────────────────────────────────────┐
│                      Angular Application                     │
├─────────────────────────────────────────────────────────────┤
│  AuthService (Keycloak OAuth2/OIDC)                         │
│    ↓ provides access token                                   │
│  ApiConfigService (Centralized API Configuration)           │
│    ↓ injects bearer token                                    │
│  SarahApiModule (All OpenAPI Clients)                       │
│    ├─ Device Service Client                                  │
│    ├─ Persons Service Client                                 │
│    ├─ Geofences Service Client                               │
│    ├─ Room Service Client                                    │
│    ├─ Monitoring Service Client                              │
│    ├─ Rules Service Client                                   │
│    └─ Speech Server Client                                   │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. AuthService (`auth.service.ts`)

Handles OAuth2/OIDC authentication with Keycloak:
- Manages login/logout flows
- Stores and refreshes JWT tokens
- Provides `getAccessToken()` method

```typescript
export class AuthService {
  public getAccessToken(): string {
    return this.oauthService.getAccessToken();
  }
}
```

### 2. ApiConfigService (`api-config.service.ts`)

**NEW** - Provides centralized configuration for all API clients:
- Creates configuration objects with bearer token authentication
- Consistent auth handling across all services
- Type-safe configuration getters

```typescript
@Injectable({ providedIn: 'root' })
export class ApiConfigService {
  constructor(private authService: AuthService) { }
  
  private createConfig(basePath: string): any {
    return {
      basePath: basePath,
      credentials: {
        bearer: () => this.authService.getAccessToken()
      }
    };
  }
  
  get deviceServiceConfig(): DeviceServiceConfig { ... }
  get personsServiceConfig(): PersonsServiceConfig { ... }
  // ... other services
}
```

### 3. SarahApiModule (`sarah-api.module.ts`)

**NEW** - Angular module that configures all API clients:
- Imports all generated API modules
- Provides base path configuration
- Centralizes API client setup

```typescript
@NgModule({
  imports: [
    DeviceServiceApiModule,
    PersonsServiceApiModule,
    // ... other API modules
  ]
})
export class SarahApiModule {
  static forRoot(): ModuleWithProviders<SarahApiModule> { ... }
}
```

### 4. Environment Configuration

All microservice endpoints are configured in `environment.ts`:

```typescript
export const environment = {
  api: {
    deviceService: 'https://localhost:5001',
    personsService: 'https://localhost:5002',
    geofencesService: 'https://localhost:5003',
    roomService: 'https://localhost:5004',
    monitoringService: 'https://localhost:5005',
    rulesService: 'https://localhost:5006',
    speechServer: 'https://localhost:5008'
  }
};
```

## Setup Instructions

### Step 1: Import SarahApiModule in AppModule

```typescript
import { SarahApiModule } from './services/sarah-api.module';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    HttpClientModule,
    SarahApiModule.forRoot(), // ← Add this
    OAuthModule.forRoot()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

### Step 2: Using API Clients with Authentication

#### Option A: Using Generated Clients Directly

The generated clients automatically receive bearer token configuration:

```typescript
import { Component, OnInit } from '@angular/core';
import { DevicesControllerService } from './services/api/device-service';

@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html'
})
export class DevicesComponent implements OnInit {
  lamps: any[] = [];
  
  constructor(private deviceClient: DevicesControllerService) { }
  
  ngOnInit(): void {
    // Bearer token is automatically included in the request
    this.deviceClient.getLamps().subscribe({
      next: (lamps) => this.lamps = lamps,
      error: (error) => console.error('Error loading lamps', error)
    });
  }
}
```

#### Option B: Using Wrapper Services (Recommended)

Create wrapper services for better abstraction:

```typescript
// device.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DevicesControllerService } from './api/device-service';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  constructor(private deviceClient: DevicesControllerService) { }
  
  getLamps(): Observable<any> {
    return this.deviceClient.getLamps();
  }
}
```

Then use the wrapper:

```typescript
constructor(private deviceService: DeviceService) { }

ngOnInit(): void {
  this.deviceService.getLamps().subscribe(lamps => {
    this.lamps = lamps;
  });
}
```

## How Bearer Token Authentication Works

### Token Flow

1. **User logs in** via `AuthService.login()`
2. **Keycloak returns JWT token**, stored by OAuthService
3. **API client makes request**:
   - ApiConfigService's `createConfig()` is called
   - It retrieves the current token via `authService.getAccessToken()`
   - Token is added as `Authorization: Bearer <token>` header
4. **Microservice validates token** with Keycloak
5. **Response is returned** to the Angular app

### Token Refresh

- The `AuthService` automatically refreshes tokens using `setupAutomaticSilentRefresh()`
- API clients always get the latest valid token
- No manual token management needed in components

## Best Practices

### 1. Use Wrapper Services

✅ **DO**: Create wrapper services for business logic
```typescript
@Injectable({ providedIn: 'root' })
export class DeviceService {
  constructor(private client: DevicesControllerService) { }
  
  // Add business logic, error handling, caching, etc.
  getLamps(): Observable<Lamp[]> {
    return this.client.getLamps().pipe(
      map(lamps => lamps.filter(lamp => lamp.isActive)),
      catchError(this.handleError)
    );
  }
}
```

❌ **DON'T**: Use generated clients directly in components
```typescript
// Avoid this - couples component to generated code
constructor(private client: DevicesControllerService) { }
```

### 2. Handle Authentication Errors

```typescript
this.deviceService.getLamps().subscribe({
  next: (lamps) => this.lamps = lamps,
  error: (error) => {
    if (error.status === 401) {
      // Token expired or invalid
      this.authService.login();
    } else {
      console.error('API Error:', error);
    }
  }
});
```

### 3. Configure Base URLs per Environment

```typescript
// environment.prod.ts
export const environment = {
  production: true,
  api: {
    deviceService: 'https://prod-server:5001',
    personsService: 'https://prod-server:5002',
    // ... other services
  }
};
```

### 4. Test with Mock Services

```typescript
// device.service.spec.ts
describe('DeviceService', () => {
  let service: DeviceService;
  let mockClient: jasmine.SpyObj<DevicesControllerService>;
  
  beforeEach(() => {
    mockClient = jasmine.createSpyObj('DevicesControllerService', ['getLamps']);
    
    TestBed.configureTestingModule({
      providers: [
        DeviceService,
        { provide: DevicesControllerService, useValue: mockClient }
      ]
    });
    
    service = TestBed.inject(DeviceService);
  });
  
  it('should fetch lamps', () => {
    const mockLamps = [{ id: 1, name: 'Living Room' }];
    mockClient.getLamps.and.returnValue(of(mockLamps));
    
    service.getLamps().subscribe(lamps => {
      expect(lamps).toEqual(mockLamps);
    });
  });
});
```

## Troubleshooting

### 401 Unauthorized Errors

**Problem**: API requests return 401 Unauthorized

**Causes & Solutions**:
1. **Not logged in**: Call `authService.login()`
2. **Token expired**: Check if automatic refresh is enabled
3. **Invalid token**: Clear storage and re-login
4. **CORS issues**: Ensure microservices allow the frontend origin

### Missing Authorization Header

**Problem**: Requests don't include bearer token

**Solution**: Verify SarahApiModule is imported with `forRoot()`:
```typescript
imports: [
  SarahApiModule.forRoot() // ← Must call forRoot()
]
```

### Token Not Updating

**Problem**: Old token is used after refresh

**Solution**: Ensure ApiConfigService uses a function (not a value):
```typescript
credentials: {
  bearer: () => this.authService.getAccessToken() // ← Function, not value
}
```

## Security Considerations

### Development vs Production

**Development** (`environment.ts`):
- Uses localhost URLs
- May use HTTP for local testing
- Self-signed certificates accepted

**Production** (`environment.prod.ts`):
- Uses HTTPS only
- Proper SSL certificates
- Secure token storage

### Token Storage

Tokens are stored in `localStorage` by default:
```typescript
this.oauthService.setStorage(localStorage);
```

For enhanced security in production, consider:
- Using `sessionStorage` for shorter-lived sessions
- Implementing token encryption
- Setting shorter token lifetimes

### API Security Headers

All generated clients support standard security headers:
- `Authorization: Bearer <token>`
- `Content-Type: application/json`
- CORS headers (configured on backend)

## Regenerating Clients

When you update microservice APIs:

```bash
# Regenerate all clients
npm run update-openapi

# Regenerate one service
node update-openapi-clients.js --service device-service
```

After regeneration:
- ✅ Bearer auth configuration is preserved (via SarahApiModule)
- ✅ Base paths remain configured (via environment)
- ✅ Your wrapper services are unaffected
- ⚠️ Review any breaking API changes

## Summary

The Sarah frontend implements a robust, consistent authentication approach:

✓ **Centralized** - All auth logic in AuthService  
✓ **Automatic** - Bearer tokens added to all requests  
✓ **Type-safe** - TypeScript interfaces for all APIs  
✓ **Consistent** - Same pattern across all 7 microservices  
✓ **Maintainable** - Wrapper services isolate generated code  
✓ **Testable** - Easy to mock and test  
✓ **Secure** - OAuth2/OIDC with Keycloak

No manual token management required in components or services!
