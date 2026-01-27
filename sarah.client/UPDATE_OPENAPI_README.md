# Update OpenAPI Clients - Quick Reference

This document provides a quick reference for updating OpenAPI clients in the Sarah frontend.

## Quick Commands

```bash
# Navigate to the sarah.client directory
cd sarah.client

# Update all OpenAPI clients (most common use case)
npm run update-openapi

# Update all clients and save the OpenAPI specs locally
npm run update-openapi:save

# Update a specific service only
node update-openapi-clients.js --service device-service

# Download specs without generating clients (for inspection)
node update-openapi-clients.js --skip-generate --save-specs

# Show all available options
node update-openapi-clients.js --help
```

## Prerequisites

Before running the update script:

1. **Start all microservices** or at least the ones you want to update
2. Ensure services are accessible at their configured ports:
   - Device Service: https://localhost:5001
   - Persons Service: https://localhost:5002
   - Geofences Service: https://localhost:5003
   - Room Service: https://localhost:5004
   - Monitoring Service: https://localhost:5005
   - Rules Service: https://localhost:5006
   - Speech Server: https://localhost:5008

## Common Use Cases

### Scenario 1: Update All Clients

When you've made changes to any microservice API:

```bash
npm run update-openapi
```

### Scenario 2: Update One Service

When you've only modified one service (faster):

```bash
node update-openapi-clients.js --service device-service
```

### Scenario 3: Save OpenAPI Specs for Reference

To keep a copy of the OpenAPI specifications:

```bash
npm run update-openapi:save
```

Specs will be saved to `openapi-specs/` directory.

### Scenario 4: Inspect OpenAPI Specs Only

To download the specs without regenerating clients:

```bash
node update-openapi-clients.js --skip-generate --save-specs
```

## Troubleshooting

### "Failed to get swagger.json"

**Problem:** The microservice is not running or not accessible.

**Solution:**
1. Check if the service is running
2. Visit `https://localhost:[PORT]/swagger` in your browser
3. If you see a certificate warning, accept it (development only)
4. Verify the port number matches the configuration

### "Error generating client"

**Problem:** The openapi-generator-cli might not be installed.

**Solution:**
```bash
npm install -g @openapitools/openapi-generator-cli@latest
```

### Certificate Errors

**Problem:** Self-signed certificates in development.

**Solution:** The script automatically handles this by setting `NODE_TLS_REJECT_UNAUTHORIZED=0`. This is only for development environments.

## What Gets Updated

When you run the update script, it:

1. **Downloads** the latest OpenAPI specification from each service
2. **Generates** TypeScript Angular client code in `src/app/services/api/[service-name]/`
3. **Overwrites** existing client code (your wrapper services are safe)
4. **Creates** model interfaces, service classes, and Angular modules

## Files Changed

After running the script, you'll see changes in:

```
src/app/services/api/
├── device-service/
├── persons-service/
├── geofences-service/
├── room-service/
├── monitoring-service/
├── rules-service/
└── speech-server/
```

**Note:** Your custom wrapper services (device.service.ts, person.service.ts, etc.) are NOT modified.

## For More Details

See [API_CLIENT_GENERATION.md](./API_CLIENT_GENERATION.md) for comprehensive documentation.
