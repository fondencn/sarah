# Sarah API Client Generation

This directory contains scripts for automatically generating TypeScript Angular API clients from the microservices' OpenAPI specifications.

## Overview

The Sarah smart home system uses a microservice architecture with 6 independent services. Each service exposes a REST API documented with OpenAPI/Swagger. These scripts automate the process of generating TypeScript clients for use in the Angular frontend.

## Prerequisites

1. **All microservices must be running** - The scripts download OpenAPI specs from live services
2. **Node.js and npm** must be installed
3. **@openapitools/openapi-generator-cli** (automatically installed if missing)

## Microservices

| Service | Port | Swagger URL |
|---------|------|-------------|
| Device Service | 5001 | https://localhost:5001/swagger |
| Persons Service | 5002 | https://localhost:5002/swagger |
| Geofences Service | 5003 | https://localhost:5003/swagger |
| Event Processing Service | 5004 | https://localhost:5004/swagger |
| Monitoring Service | 5005 | https://localhost:5005/swagger |
| Rules Service | 5006 | https://localhost:5006/swagger |

## Usage

### Option 1: Using the Shell Script (Recommended)

```bash
# Make the script executable (first time only)
chmod +x generate-api-clients.sh

# Run the generator
./generate-api-clients.sh
```

### Option 2: Using Node.js directly

```bash
# Run the Node.js script
node generate-api-clients.js
```

## What Gets Generated

The scripts will:

1. Download OpenAPI specifications from each running microservice
2. Generate TypeScript Angular clients in `src/app/services/api/[service-name]/`
3. Each generated client includes:
   - API service classes with typed methods
   - Model interfaces for request/response objects
   - Configuration classes
   - Angular module definitions

## Generated Directory Structure

```
src/app/services/
├── api/
│   ├── device-service/          # Generated Device Service client
│   │   ├── api/
│   │   │   └── devicesController.service.ts
│   │   ├── model/
│   │   │   └── *.model.ts
│   │   └── ...
│   ├── persons-service/         # Generated Persons Service client
│   ├── geofences-service/       # Generated Geofences Service client
│   ├── eventprocessing-service/ # Generated Event Processing Service client
│   ├── monitoring-service/      # Generated Monitoring Service client
│   └── rules-service/           # Generated Rules Service client
├── device.service.ts            # Wrapper service for Device API
├── person.service.ts            # Wrapper service for Persons API
├── geofence.service.ts          # Wrapper service for Geofences API
├── event-processing.service.ts  # Wrapper service for Event Processing API
├── monitoring.service.ts        # Wrapper service for Monitoring API
└── rules.service.ts             # Wrapper service for Rules API
```

## Wrapper Services

Pre-built wrapper services are provided in `src/app/services/`. These services:

- Provide a simplified, application-specific interface
- Hide implementation details of the generated clients
- Make it easier to mock during testing
- Allow for additional business logic

### Example Usage

```typescript
import { Component, OnInit } from '@angular/core';
import { DeviceService } from './services/device.service';

@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html'
})
export class DevicesComponent implements OnInit {

  lamps: any[] = [];

  constructor(private deviceService: DeviceService) { }

  ngOnInit() {
    this.deviceService.getLamps().subscribe(
      lamps => this.lamps = lamps,
      error => console.error('Error loading lamps', error)
    );
  }
}
```

## Configuration

### Authentication

The generated clients support Bearer token authentication (JWT). Configure this in your Angular app:

```typescript
import { Configuration } from './services/api/device-service';

const apiConfig = new Configuration({
  basePath: 'https://localhost:5001',
  accessToken: () => this.authService.getAccessToken()
});
```

### Base URLs

By default, clients use the URLs where the OpenAPI specs were downloaded from. Update these in your app configuration:

```typescript
// In environment.ts
export const environment = {
  api: {
    deviceService: 'https://localhost:5001',
    personsService: 'https://localhost:5002',
    geofencesService: 'https://localhost:5003',
    eventProcessingService: 'https://localhost:5004',
    monitoringService: 'https://localhost:5005',
    rulesService: 'https://localhost:5006'
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

## Scripts

### generate-api-clients.js

Node.js script that:
- Downloads OpenAPI specs from all microservices
- Generates TypeScript Angular clients using openapi-generator-cli
- Handles errors and provides detailed progress output

### generate-api-clients.sh

Shell script wrapper that:
- Checks prerequisites
- Installs openapi-generator-cli if needed
- Runs the Node.js generator
- Provides usage instructions

## Integration with Angular Build

To automatically regenerate clients during development, add to `package.json`:

```json
{
  "scripts": {
    "generate-clients": "node generate-api-clients.js",
    "prestart": "npm run generate-clients",
    "start": "ng serve"
  }
}
```

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

