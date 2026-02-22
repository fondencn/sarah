# Sarah API Client Generation

This directory contains a unified script for automatically generating TypeScript Angular API clients from the microservices' OpenAPI specifications.

## Overview

The Sarah smart home system uses a microservice architecture with 7 independent services. Each service exposes a REST API documented with OpenAPI/Swagger. The `update-openapi-clients.js` script automates the process of downloading OpenAPI specs and generating TypeScript clients for use in the Angular frontend.

## Prerequisites

1. **All microservices must be running** - The scripts download OpenAPI specs from live services
2. **Node.js and npm** must be installed
3. **@openapitools/openapi-generator-cli** (automatically installed if missing)

## Microservices

| Service | Port | Swagger URL | Description |
|---------|------|-------------|-------------|
| Device Service | 5001 | http://localhost:5001/swagger | Manages smart home devices |
| Persons Service | 5002 | http://localhost:5002/swagger | Manages persons and user profiles |
| Geofences Service | 5003 | http://localhost:5003/swagger | Manages geofences and location automation |
| Room Service | 5004 | http://localhost:5004/swagger | Manages rooms and device organization |
| Monitoring Service | 5005 | http://localhost:5005/swagger | System monitoring and health checks |
| Rules Service | 5006 | http://localhost:5006/swagger | Automation rules engine |
| Speech Server | 5008 | http://localhost:5008/swagger | Voice recognition and text-to-speech |

## Usage

### Quick Start

The easiest way to update all API clients is using npm scripts:

```bash
# Update all OpenAPI clients
npm run update-openapi

# Update all clients and save OpenAPI specs
npm run update-openapi:save
```

### Advanced Usage

The `update-openapi-clients.js` script supports several options:

```bash
# Generate all clients (default)
node update-openapi-clients.js

# Generate a specific service only
node update-openapi-clients.js --service device-service

# Save OpenAPI specs to openapi-specs/ directory
node update-openapi-clients.js --save-specs

# Download specs only, skip generation
node update-openapi-clients.js --skip-generate --save-specs

# Show help
node update-openapi-clients.js --help
```

### Available npm Scripts

| Script | Command | Description |
|--------|---------|-------------|
| `update-openapi` | `npm run update-openapi` | Generate all OpenAPI clients |
| `update-openapi:save` | `npm run update-openapi:save` | Generate clients and save specs |
| `generate-clients` | `npm run generate-clients` | Alias for update-openapi |
| `update-api` | `npm run update-api` | Alias for update-openapi |

## What Gets Generated

The script will:

1. **Download** OpenAPI specifications from each running microservice
2. **Generate** TypeScript Angular clients in `src/app/services/api/[service-name]/`
3. Each generated client includes:
   - API service classes with typed methods
   - Model interfaces for request/response objects
   - Configuration classes with bearer token support
   - Angular module definitions
   - Automatic providedIn: 'root' for services

### Generated Clients Include:

- ✅ **Type-safe API methods** - Full TypeScript typing
- ✅ **Bearer token authentication** - Automatic OAuth2/OIDC integration
- ✅ **Angular 18 compatibility** - Latest Angular features
- ✅ **providedIn: 'root'** - Singleton services
- ✅ **RxJS Observables** - Reactive programming support
- ✅ **Error handling** - HTTP error responses

## Generated Directory Structure

```
sarah.client/
├── openapi-specs/               # Saved OpenAPI specs (optional, with --save-specs)
│   ├── device-service.json
│   ├── persons-service.json
│   ├── geofences-service.json
│   ├── room-service.json
│   ├── monitoring-service.json
│   ├── rules-service.json
│   └── speech-server.json
└── src/app/services/
    ├── api/
    │   ├── device-service/          # Generated Device Service client
    │   │   ├── api/
    │   │   │   └── *.service.ts
    │   │   ├── model/
    │   │   │   └── *.model.ts
    │   │   └── ...
    │   ├── persons-service/         # Generated Persons Service client
    │   ├── geofences-service/       # Generated Geofences Service client
    │   ├── room-service/            # Generated Room Service client
    │   ├── monitoring-service/      # Generated Monitoring Service client
    │   ├── rules-service/           # Generated Rules Service client
    │   └── speech-server/           # Generated Speech Server client
    ├── device.service.ts            # Wrapper service for Device API
    ├── person.service.ts            # Wrapper service for Persons API
    ├── geofence.service.ts          # Wrapper service for Geofences API
    ├── monitoring.service.ts        # Wrapper service for Monitoring API
    └── rules.service.ts             # Wrapper service for Rules API
```

## Authentication & Security

All generated clients support **bearer token authentication** automatically:

### Setup (One-Time)

Import `SarahApiModule` in your `app.module.ts`:

```typescript
import { SarahApiModule } from './services/sarah-api.module';

@NgModule({
  imports: [
    SarahApiModule.forRoot() // ← Configures all clients with auth
  ]
})
export class AppModule { }
```

### How It Works

1. **AuthService** manages OAuth2/OIDC authentication with Keycloak
2. **AuthInterceptor** automatically adds bearer tokens to all API requests
3. **Environment config** defines all microservice endpoints
4. **Generated clients** work seamlessly with authentication

**No manual token management required!**

For usage examples, see [API_USAGE_EXAMPLES.md](./API_USAGE_EXAMPLES.md).

### Example Usage

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
  
  ngOnInit() {
    // Bearer token is automatically added to the request
    this.deviceClient.getLamps().subscribe({
      next: (lamps) => this.lamps = lamps,
      error: (error) => console.error('Error loading lamps', error)
    });
  }
}
```

**Note:** Bearer authentication is handled automatically. No manual token management needed!

## Wrapper Services (Recommended Pattern)

Create wrapper services for better abstraction and testability:

```typescript
// device.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DevicesControllerService } from './api/device-service';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  constructor(private client: DevicesControllerService) { }
  
  getLamps(): Observable<any> {
    return this.client.getLamps();
  }
}
```

Benefits:
- Simplified interface for your application
- Easy to mock during testing
- Allows adding business logic
- Decouples components from generated code

## Configuration

### Authentication

All generated clients automatically support Bearer token authentication via the `AuthInterceptor`.

The interceptor:
- Adds `Authorization: Bearer <token>` header to all microservice requests
- Retrieves tokens from `AuthService` (Keycloak OAuth2/OIDC)
- Handles 401 errors by redirecting to login
- Works consistently across all 7 microservices

**Setup once in AppModule:**
```typescript
import { SarahApiModule } from './services/sarah-api.module';

@NgModule({
  imports: [
    SarahApiModule.forRoot()
  ]
})
export class AppModule { }
```

### Base URLs

All microservice endpoints are configured in environment files:

```typescript
// In environment.ts
export const environment = {
  api: {
    deviceService: 'http://localhost:5001',
    personsService: 'http://localhost:5002',
    geofencesService: 'http://localhost:5003',
    eventProcessingService: 'http://localhost:5004',
    monitoringService: 'http://localhost:5005',
    rulesService: 'http://localhost:5006'
  }
};
```

## When to Regenerate Clients

Regenerate the API clients whenever:

1. A microservice's API endpoints change
2. Request/response models are modified
3. New endpoints are added
4. Authentication requirements change

## Troubleshooting

### "Failed to get swagger.json"

- **Cause**: Microservice is not running
- **Solution**: Start all microservices before running the generator
- **Check**: Visit `https://localhost:[PORT]/swagger` in your browser

### "Error generating client"

- **Cause**: openapi-generator-cli not installed or outdated
- **Solution**: Run `npm install -g @openapitools/openapi-generator-cli@latest`

### SSL Certificate Errors

- **Cause**: Development uses self-signed certificates
- **Solution**: The scripts automatically disable SSL verification (NODE_TLS_REJECT_UNAUTHORIZED=0)
- **Note**: This is only for development; production should use proper certificates

## The Script: update-openapi-clients.js

A comprehensive Node.js script that:
- Downloads OpenAPI specs from all Sarah microservices
- Generates TypeScript Angular clients using openapi-generator-cli
- Supports selective service updates
- Optionally saves OpenAPI specs for offline use
- Provides detailed progress output and error handling
- Supports both all-services and single-service mode

### Features

✓ **Flexible**: Update all services or just one  
✓ **Spec Saving**: Optionally save OpenAPI specs locally  
✓ **Offline Mode**: Download specs without generating clients  
✓ **Error Handling**: Clear error messages with troubleshooting hints  
✓ **Progress Tracking**: Detailed output for each step  
✓ **Auto-configuration**: Discovers all services automatically

## Integration with Angular Build

The scripts are already integrated in `package.json`. If you want to automatically regenerate clients before starting the dev server, you can modify the `prestart` script:

```json
{
  "scripts": {
    "prestart": "node aspnetcore-https && node update-openapi-clients.js",
    "start": "ng serve"
  }
}
```

**Note:** This is not enabled by default as it requires all microservices to be running.

## Further Reading

- [OpenAPI Generator Documentation](https://openapi-generator.tech/docs/generators/typescript-angular)
- [Swagger/OpenAPI Specification](https://swagger.io/specification/)
- [Angular HttpClient Guide](https://angular.io/guide/http)

## Support

For issues or questions:
1. Check that all microservices are running and accessible
2. Verify OpenAPI specs are valid at `/swagger/v1/swagger.json` endpoints
3. Review the generated code in `src/app/services/api/` directories
4. Check Angular console for runtime errors

