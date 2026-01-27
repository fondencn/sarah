# Sarah OpenAPI Client System - Complete Setup Guide

This document provides a complete overview of the Sarah OpenAPI client system, including setup, usage, and maintenance.

## 🎯 Quick Start

### 1. Generate API Clients

```bash
cd sarah.client
npm run update-openapi
```

### 2. Import in AppModule

```typescript
import { SarahApiModule } from './services/sarah-api.module';

@NgModule({
  imports: [
    SarahApiModule.forRoot()
  ]
})
export class AppModule { }
```

### 3. Use in Components

```typescript
import { DevicesControllerService } from './services/api/device-service';

@Component({...})
export class MyComponent {
  constructor(private deviceClient: DevicesControllerService) { }
  
  loadDevices() {
    this.deviceClient.getLamps().subscribe(lamps => {
      // Bearer token is automatically included!
      console.log(lamps);
    });
  }
}
```

**That's it!** Authentication is handled automatically.

## 📚 Documentation Structure

### Primary Documents

| Document | Purpose | When to Read |
|----------|---------|--------------|
| **UPDATE_OPENAPI_README.md** | Quick reference for updating clients | When regenerating clients |
| **API_CLIENT_GENERATION.md** | Detailed generation documentation | First time setup |
| **API_USAGE_EXAMPLES.md** | Code examples and patterns | Writing code |
| **COMPLETE_SETUP.md** | This file - overview of everything | Getting started |

### Quick Links by Task

**I want to...**
- **Update API clients** → [UPDATE_OPENAPI_README.md](./UPDATE_OPENAPI_README.md)
- **Understand authentication** → See "System Architecture" section below
- **See code examples** → [API_USAGE_EXAMPLES.md](./API_USAGE_EXAMPLES.md)
- **Generate clients first time** → [API_CLIENT_GENERATION.md](./API_CLIENT_GENERATION.md)
- **Get complete overview** → This document

## 🏗️ System Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Angular Frontend                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────┐        │
│  │  Authentication Layer                            │        │
│  │  - AuthService (Keycloak OAuth2/OIDC)          │        │
│  │  - AuthInterceptor (Auto Bearer Token)          │        │
│  └─────────────────────────────────────────────────┘        │
│                         ↓                                     │
│  ┌─────────────────────────────────────────────────┐        │
│  │  SarahApiModule                                  │        │
│  │  - Configures all API clients                   │        │
│  │  - Provides base paths                          │        │
│  │  - Registers interceptor                        │        │
│  └─────────────────────────────────────────────────┘        │
│                         ↓                                     │
│  ┌─────────────────────────────────────────────────┐        │
│  │  Generated OpenAPI Clients (7 services)         │        │
│  │  ├─ Device Service Client                       │        │
│  │  ├─ Persons Service Client                      │        │
│  │  ├─ Geofences Service Client                    │        │
│  │  ├─ Room Service Client                         │        │
│  │  ├─ Monitoring Service Client                   │        │
│  │  ├─ Rules Service Client                        │        │
│  │  └─ Speech Server Client                        │        │
│  └─────────────────────────────────────────────────┘        │
│                         ↓                                     │
│  ┌─────────────────────────────────────────────────┐        │
│  │  Your Components & Services                      │        │
│  │  - Use generated clients                        │        │
│  │  - Create wrapper services                      │        │
│  │  - Implement business logic                     │        │
│  └─────────────────────────────────────────────────┘        │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

```
User Action in Component
    ↓
Wrapper Service (optional but recommended)
    ↓
Generated API Client
    ↓
AuthInterceptor (adds Bearer token)
    ↓
HTTP Request to Microservice
    ↓
Keycloak validates token
    ↓
Microservice processes request
    ↓
Response returns through Observable
    ↓
Component updates UI
```

## 🔑 Key Files & Their Purposes

### Core Configuration Files

| File | Purpose | Edit Frequency |
|------|---------|----------------|
| `update-openapi-clients.js` | Script to download specs and generate clients | Rarely - already configured |
| `src/environments/environment.ts` | Dev environment config | When adding services |
| `src/environments/environment.prod.ts` | Prod environment config | When deploying |
| `src/app/services/sarah-api.module.ts` | API module registration | When adding services |
| `src/app/services/auth.interceptor.ts` | Auto bearer token injection | Rarely - already working |
| `package.json` | npm scripts | Rarely - scripts configured |

### Documentation Files

| File | Content | Size |
|------|---------|------|
| `API_CLIENT_GENERATION.md` | Generation process details | ~6 KB |
| `API_USAGE_EXAMPLES.md` | Code examples | ~14 KB |
| `UPDATE_OPENAPI_README.md` | Quick reference | ~3 KB |
| `COMPLETE_SETUP.md` | This overview | ~8 KB |

### Generated Files (Don't Edit Manually)

```
src/app/services/api/
├── device-service/        # Generated by script
├── persons-service/       # Generated by script
├── geofences-service/     # Generated by script
├── room-service/          # Generated by script
├── monitoring-service/    # Generated by script
├── rules-service/         # Generated by script
└── speech-server/         # Generated by script
```

⚠️ **Never edit files in `src/app/services/api/` directories** - they are regenerated when you run the update script.

## 🔧 Common Tasks

### Task 1: Update API Clients After Backend Changes

```bash
# Update all clients
npm run update-openapi

# Or update just one service
node update-openapi-clients.js --service device-service
```

### Task 2: Add a New Microservice

1. **Update the script** (`update-openapi-clients.js`):
```javascript
const microservices = [
  // ... existing services
  {
    name: 'new-service',
    port: 5009,
    title: 'New Service',
    outputDir: './src/app/services/api/new-service',
    description: 'Description of new service'
  }
];
```

2. **Update environment config** (`src/environments/environment.ts`):
```typescript
export const environment = {
  api: {
    // ... existing services
    newService: 'https://localhost:5009'
  }
};
```

3. **Update SarahApiModule** (`src/app/services/sarah-api.module.ts`):
```typescript
import { ApiModule as NewServiceApiModule, BASE_PATH as NEW_SERVICE_BASE_PATH } from './api/new-service';

@NgModule({
  imports: [
    // ... existing imports
    NewServiceApiModule
  ]
})
export class SarahApiModule {
  static forRoot(): ModuleWithProviders<SarahApiModule> {
    return {
      ngModule: SarahApiModule,
      providers: [
        // ... existing providers
        { provide: NEW_SERVICE_BASE_PATH, useValue: environment.api.newService }
      ]
    };
  }
}
```

4. **Generate the client**:
```bash
node update-openapi-clients.js --service new-service
```

### Task 3: Create a Wrapper Service

```typescript
// new-service-wrapper.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { NewServiceControllerService } from './api/new-service';

@Injectable({ providedIn: 'root' })
export class NewServiceWrapper {
  
  constructor(private client: NewServiceControllerService) { }
  
  getData(): Observable<any> {
    return this.client.getData().pipe(
      map(data => {
        // Add business logic here
        return data;
      }),
      catchError(error => {
        console.error('Error in NewServiceWrapper:', error);
        throw error;
      })
    );
  }
}
```

### Task 4: Debug Authentication Issues

1. **Check if user is logged in**:
```typescript
constructor(private authService: AuthService) {
  console.log('Logged in:', this.authService.isLoggedIn());
  console.log('Token:', this.authService.getAccessToken());
}
```

2. **Check interceptor is registered**:
```typescript
// In app.module.ts
imports: [
  SarahApiModule.forRoot() // ← Must be called
]
```

3. **Check environment URLs**:
```typescript
// Verify URLs in browser console
console.log(environment.api);
```

4. **Check network requests**:
- Open browser DevTools → Network tab
- Make an API request
- Look for `Authorization: Bearer <token>` header

## 🎨 Best Practices

### ✅ DO

- ✅ Use wrapper services for business logic
- ✅ Handle errors at component level
- ✅ Import `SarahApiModule.forRoot()` once in AppModule
- ✅ Use TypeScript types from generated models
- ✅ Test with mocked services
- ✅ Regenerate clients after API changes

### ❌ DON'T

- ❌ Edit generated files in `src/app/services/api/`
- ❌ Manually add bearer tokens (interceptor does this)
- ❌ Use generated clients directly in components (use wrappers)
- ❌ Commit generated files to git (already ignored)
- ❌ Hardcode API URLs (use environment config)

## 🧪 Testing

### Unit Test Example

```typescript
describe('MyService', () => {
  let service: MyService;
  let mockClient: jasmine.SpyObj<DevicesControllerService>;
  
  beforeEach(() => {
    mockClient = jasmine.createSpyObj('DevicesControllerService', ['getLamps']);
    
    TestBed.configureTestingModule({
      providers: [
        MyService,
        { provide: DevicesControllerService, useValue: mockClient }
      ]
    });
    
    service = TestBed.inject(MyService);
  });
  
  it('should work', () => {
    mockClient.getLamps.and.returnValue(of([]));
    service.getLamps().subscribe();
    expect(mockClient.getLamps).toHaveBeenCalled();
  });
});
```

## 📦 NPM Scripts Reference

| Script | Command | Description |
|--------|---------|-------------|
| `update-openapi` | `npm run update-openapi` | Generate all API clients |
| `update-openapi:save` | `npm run update-openapi:save` | Generate clients + save specs |
| `generate-clients` | `npm run generate-clients` | Alias for update-openapi |
| `update-api` | `npm run update-api` | Alias for update-openapi |

## 🔍 Troubleshooting

### Problem: "401 Unauthorized" errors

**Solution:**
1. Check if logged in: `authService.isLoggedIn()`
2. Check token: `authService.getAccessToken()`
3. Clear browser storage and re-login
4. Verify Keycloak is running

### Problem: "No bearer token in requests"

**Solution:**
1. Verify `SarahApiModule.forRoot()` is imported
2. Check interceptor registration in providers
3. Verify AuthService is returning a token

### Problem: "Service not found after generation"

**Solution:**
1. Verify service is running on correct port
2. Check OpenAPI spec is accessible: `https://localhost:[PORT]/swagger/v1/swagger.json`
3. Accept self-signed certificate in browser
4. Re-run generation script

### Problem: "TypeScript compilation errors"

**Solution:**
1. Run `npm install` to ensure all dependencies are installed
2. Check that generated files are in correct directories
3. Verify imports in your code
4. Run `ng build` to see detailed errors

## 📈 System Benefits

### For Developers

- ✅ **No manual API coding** - Generated from OpenAPI specs
- ✅ **Type safety** - Full TypeScript support
- ✅ **Auto authentication** - Bearer tokens handled automatically
- ✅ **Easy testing** - Mock services with Jasmine
- ✅ **Consistent patterns** - Same approach for all 7 services

### For the Project

- ✅ **Maintainable** - Clear separation of concerns
- ✅ **Scalable** - Easy to add new services
- ✅ **Documented** - Comprehensive guides
- ✅ **Best practices** - Follows Angular style guide
- ✅ **Secure** - OAuth2/OIDC with Keycloak

## 🎓 Learning Path

**New to the project?** Follow this order:

1. Read this file (COMPLETE_SETUP.md) - Overview
2. Read UPDATE_OPENAPI_README.md - Learn how to update clients
3. Review "System Architecture" section - Understand authentication
4. Read API_USAGE_EXAMPLES.md - See code examples
5. Start coding with the examples!

**Need to regenerate clients?** → UPDATE_OPENAPI_README.md

**Need code examples?** → API_USAGE_EXAMPLES.md

**Debugging auth issues?** → See "Troubleshooting" section below

## 📞 Support

For issues or questions:
1. Check this documentation first
2. Review the specific guide for your task
3. Check browser console for errors
4. Verify all microservices are running
5. Review network requests in DevTools

## 🎉 Summary

You now have a complete, production-ready OpenAPI client system with:

✅ **7 microservices** configured and ready  
✅ **Automatic authentication** via HTTP interceptor  
✅ **Type-safe clients** generated from OpenAPI specs  
✅ **Comprehensive documentation** for all scenarios  
✅ **Best practices** built-in  
✅ **Easy maintenance** with update scripts  
✅ **Testing support** with mockable services  

**Start building features - the infrastructure is ready!**
