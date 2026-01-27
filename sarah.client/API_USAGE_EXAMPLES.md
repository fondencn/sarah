# Sarah OpenAPI Client - Usage Examples

This document provides practical examples of using the generated OpenAPI clients with proper authentication.

## Table of Contents
- [Setup](#setup)
- [Basic Usage](#basic-usage)
- [Advanced Patterns](#advanced-patterns)
- [Error Handling](#error-handling)
- [Testing](#testing)

## Setup

### 1. Import SarahApiModule in AppModule

```typescript
// app.module.ts
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { OAuthModule } from 'angular-oauth2-oidc';
import { SarahApiModule } from './services/sarah-api.module';
import { AppComponent } from './app.component';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    HttpClientModule,
    OAuthModule.forRoot(),
    SarahApiModule.forRoot() // ← Configures all API clients with auth
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

**That's it!** Bearer token authentication is now configured for all API clients.

## Basic Usage

### Example 1: List All Devices

```typescript
// devices.component.ts
import { Component, OnInit } from '@angular/core';
import { DevicesControllerService } from './services/api/device-service';

@Component({
  selector: 'app-devices',
  template: `
    <h2>My Devices</h2>
    <div *ngFor="let lamp of lamps">
      {{ lamp.name }} - {{ lamp.state }}
    </div>
  `
})
export class DevicesComponent implements OnInit {
  lamps: any[] = [];
  
  constructor(private deviceClient: DevicesControllerService) { }
  
  ngOnInit(): void {
    // Bearer token is automatically added by AuthInterceptor
    this.deviceClient.getLamps().subscribe({
      next: (lamps) => {
        this.lamps = lamps;
        console.log('Loaded lamps:', lamps);
      },
      error: (error) => {
        console.error('Error loading lamps:', error);
        // 401 errors automatically trigger re-login
      }
    });
  }
}
```

### Example 2: Control a Device

```typescript
// lamp-control.component.ts
import { Component } from '@angular/core';
import { DevicesControllerService } from './services/api/device-service';

@Component({
  selector: 'app-lamp-control',
  template: `
    <button (click)="turnOn()">Turn On</button>
    <button (click)="turnOff()">Turn Off</button>
  `
})
export class LampControlComponent {
  lampId = 'lamp-123';
  
  constructor(private deviceClient: DevicesControllerService) { }
  
  turnOn(): void {
    this.deviceClient.turnOnLamp(this.lampId).subscribe({
      next: () => console.log('Lamp turned on'),
      error: (error) => console.error('Failed to turn on lamp:', error)
    });
  }
  
  turnOff(): void {
    this.deviceClient.turnOffLamp(this.lampId).subscribe({
      next: () => console.log('Lamp turned off'),
      error: (error) => console.error('Failed to turn off lamp:', error)
    });
  }
}
```

### Example 3: Using Multiple Services

```typescript
// dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { DevicesControllerService } from './services/api/device-service';
import { PersonsControllerService } from './services/api/persons-service';
import { MonitoringControllerService } from './services/api/monitoring-service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  deviceCount = 0;
  personCount = 0;
  systemStatus = '';
  
  constructor(
    private deviceClient: DevicesControllerService,
    private personsClient: PersonsControllerService,
    private monitoringClient: MonitoringControllerService
  ) { }
  
  ngOnInit(): void {
    // Load data from multiple services in parallel
    forkJoin({
      devices: this.deviceClient.getLamps(),
      persons: this.personsClient.getAllPersons(),
      status: this.monitoringClient.getSystemStatus()
    }).subscribe({
      next: (results) => {
        this.deviceCount = results.devices.length;
        this.personCount = results.persons.length;
        this.systemStatus = results.status.health;
      },
      error: (error) => console.error('Dashboard load error:', error)
    });
  }
}
```

## Advanced Patterns

### Pattern 1: Wrapper Service (Recommended)

Create a wrapper service to add business logic and make testing easier:

```typescript
// device.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, catchError, retry } from 'rxjs/operators';
import { DevicesControllerService, LampModel } from './api/device-service';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  
  constructor(private client: DevicesControllerService) { }
  
  /**
   * Get all active lamps
   */
  getActiveLamps(): Observable<LampModel[]> {
    return this.client.getLamps().pipe(
      map(lamps => lamps.filter(lamp => lamp.isActive)),
      retry(2), // Retry failed requests twice
      catchError(this.handleError)
    );
  }
  
  /**
   * Toggle lamp state
   */
  toggleLamp(lampId: string, currentState: boolean): Observable<void> {
    return currentState 
      ? this.client.turnOffLamp(lampId)
      : this.client.turnOnLamp(lampId);
  }
  
  /**
   * Get lamp by room
   */
  getLampsByRoom(roomId: string): Observable<LampModel[]> {
    return this.client.getLamps().pipe(
      map(lamps => lamps.filter(lamp => lamp.roomId === roomId))
    );
  }
  
  private handleError(error: any): Observable<never> {
    console.error('DeviceService error:', error);
    throw error;
  }
}
```

Use the wrapper in components:

```typescript
@Component({...})
export class DevicesComponent {
  constructor(private deviceService: DeviceService) { } // ← Use wrapper
  
  ngOnInit(): void {
    this.deviceService.getActiveLamps().subscribe(...);
  }
}
```

### Pattern 2: Caching with RxJS

Add caching to reduce API calls:

```typescript
// cached-device.service.ts
import { Injectable } from '@angular/core';
import { Observable, of, timer } from 'rxjs';
import { shareReplay, switchMap } from 'rxjs/operators';
import { DevicesControllerService, LampModel } from './api/device-service';

@Injectable({ providedIn: 'root' })
export class CachedDeviceService {
  private lampsCache$: Observable<LampModel[]> | null = null;
  private cacheTime = 60000; // 60 seconds
  
  constructor(private client: DevicesControllerService) { }
  
  /**
   * Get lamps with automatic cache refresh
   */
  getLamps(): Observable<LampModel[]> {
    if (!this.lampsCache$) {
      // Create cache that auto-refreshes every 60 seconds
      this.lampsCache$ = timer(0, this.cacheTime).pipe(
        switchMap(() => this.client.getLamps()),
        shareReplay(1)
      );
    }
    return this.lampsCache$;
  }
  
  /**
   * Invalidate cache (call after updates)
   */
  invalidateCache(): void {
    this.lampsCache$ = null;
  }
}
```

### Pattern 3: State Management with RxJS BehaviorSubject

```typescript
// device-state.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { DevicesControllerService, LampModel } from './api/device-service';

@Injectable({ providedIn: 'root' })
export class DeviceStateService {
  private lampsSubject = new BehaviorSubject<LampModel[]>([]);
  public lamps$ = this.lampsSubject.asObservable();
  
  constructor(private client: DevicesControllerService) { }
  
  /**
   * Load and broadcast lamps
   */
  loadLamps(): void {
    this.client.getLamps().subscribe(lamps => {
      this.lampsSubject.next(lamps);
    });
  }
  
  /**
   * Update lamp and refresh state
   */
  updateLamp(lampId: string, updates: Partial<LampModel>): Observable<void> {
    return this.client.updateLamp(lampId, updates).pipe(
      tap(() => this.loadLamps()) // Refresh after update
    );
  }
  
  /**
   * Get current lamps value (synchronous)
   */
  get currentLamps(): LampModel[] {
    return this.lampsSubject.value;
  }
}
```

Use in components:

```typescript
@Component({
  selector: 'app-devices',
  template: `
    <div *ngFor="let lamp of lamps$ | async">
      {{ lamp.name }}
    </div>
  `
})
export class DevicesComponent implements OnInit {
  lamps$ = this.deviceState.lamps$;
  
  constructor(private deviceState: DeviceStateService) { }
  
  ngOnInit(): void {
    this.deviceState.loadLamps();
  }
}
```

## Error Handling

### Pattern 1: Global Error Handler

```typescript
// global-error-handler.ts
import { ErrorHandler, Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from './services/auth.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  
  constructor(private authService: AuthService) { }
  
  handleError(error: Error | HttpErrorResponse): void {
    if (error instanceof HttpErrorResponse) {
      // HTTP error
      switch (error.status) {
        case 401:
          console.error('Unauthorized - redirecting to login');
          this.authService.login();
          break;
        case 403:
          console.error('Forbidden - insufficient permissions');
          break;
        case 404:
          console.error('Resource not found');
          break;
        case 500:
          console.error('Server error:', error.message);
          break;
        default:
          console.error('HTTP error:', error);
      }
    } else {
      // Client-side error
      console.error('Client error:', error);
    }
  }
}

// Register in AppModule
@NgModule({
  providers: [
    { provide: ErrorHandler, useClass: GlobalErrorHandler }
  ]
})
export class AppModule { }
```

### Pattern 2: Component-Level Error Handling

```typescript
@Component({...})
export class DevicesComponent {
  lamps: LampModel[] = [];
  loading = false;
  error: string | null = null;
  
  constructor(private deviceClient: DevicesControllerService) { }
  
  loadLamps(): void {
    this.loading = true;
    this.error = null;
    
    this.deviceClient.getLamps().subscribe({
      next: (lamps) => {
        this.lamps = lamps;
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
        if (error.status === 404) {
          this.error = 'No devices found';
        } else if (error.status >= 500) {
          this.error = 'Server error. Please try again later.';
        } else {
          this.error = 'Failed to load devices';
        }
        console.error('Load error:', error);
      }
    });
  }
}
```

## Testing

### Pattern 1: Unit Testing with Mock Service

```typescript
// device.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { DeviceService } from './device.service';
import { DevicesControllerService } from './api/device-service';

describe('DeviceService', () => {
  let service: DeviceService;
  let mockClient: jasmine.SpyObj<DevicesControllerService>;
  
  beforeEach(() => {
    // Create mock client
    mockClient = jasmine.createSpyObj('DevicesControllerService', [
      'getLamps',
      'turnOnLamp',
      'turnOffLamp'
    ]);
    
    TestBed.configureTestingModule({
      providers: [
        DeviceService,
        { provide: DevicesControllerService, useValue: mockClient }
      ]
    });
    
    service = TestBed.inject(DeviceService);
  });
  
  it('should fetch lamps', () => {
    const mockLamps = [
      { id: '1', name: 'Living Room', isActive: true },
      { id: '2', name: 'Bedroom', isActive: false }
    ];
    
    mockClient.getLamps.and.returnValue(of(mockLamps));
    
    service.getActiveLamps().subscribe(lamps => {
      expect(lamps.length).toBe(1);
      expect(lamps[0].name).toBe('Living Room');
    });
  });
  
  it('should handle errors', () => {
    mockClient.getLamps.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    
    service.getActiveLamps().subscribe({
      next: () => fail('Should have failed'),
      error: (error) => expect(error).toBeTruthy()
    });
  });
});
```

### Pattern 2: Integration Testing

```typescript
// devices.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { DevicesComponent } from './devices.component';
import { DeviceService } from './services/device.service';

describe('DevicesComponent', () => {
  let component: DevicesComponent;
  let fixture: ComponentFixture<DevicesComponent>;
  let deviceService: jasmine.SpyObj<DeviceService>;
  
  beforeEach(() => {
    const deviceServiceSpy = jasmine.createSpyObj('DeviceService', ['getLamps']);
    
    TestBed.configureTestingModule({
      declarations: [DevicesComponent],
      imports: [HttpClientTestingModule],
      providers: [
        { provide: DeviceService, useValue: deviceServiceSpy }
      ]
    });
    
    fixture = TestBed.createComponent(DevicesComponent);
    component = fixture.componentInstance;
    deviceService = TestBed.inject(DeviceService) as jasmine.SpyObj<DeviceService>;
  });
  
  it('should load lamps on init', () => {
    const mockLamps = [{ id: '1', name: 'Test Lamp' }];
    deviceService.getLamps.and.returnValue(of(mockLamps));
    
    fixture.detectChanges(); // Triggers ngOnInit
    
    expect(component.lamps).toEqual(mockLamps);
  });
});
```

## Summary

Key takeaways:

✓ **No manual auth** - Bearer tokens added automatically via interceptor  
✓ **Use wrapper services** - Better abstraction and testability  
✓ **Handle errors** - Implement proper error handling at component level  
✓ **Cache when appropriate** - Reduce unnecessary API calls  
✓ **Test with mocks** - Easy to test with jasmine spies  
✓ **Follow patterns** - Use RxJS operators for clean, reactive code

All examples work out of the box after importing `SarahApiModule.forRoot()` in your AppModule!
