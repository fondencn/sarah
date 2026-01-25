# Sarah AppHost - Aspire Orchestration

This project is the Aspire orchestration host for the Sarah Smart Home Management System. It configures and coordinates all microservices, databases, message brokers, and the identity provider.

## Overview

The AppHost uses .NET Aspire to orchestrate the following services:

- **Keycloak** - Identity and Access Management (OAuth2/OIDC)
- **RabbitMQ** - Message broker for event-driven communication
- **PostgreSQL** - Database instances for each microservice
- **Microservices** - All Sarah backend services
- **Angular Frontend** - The sarah.client SPA

## Configuration

### Keycloak Configuration

Keycloak is automatically configured with a pre-imported realm on startup. The configuration is managed through:

#### appsettings.json

```json
{
  "Keycloak": {
    "AdminUser": "admin",
    "AdminPassword": "ChangeMe123!",
    "TestUser": {
      "Username": "sarah-admin",
      "Password": "TestPassword123!",
      "Email": "admin@sarah.local",
      "FirstName": "Sarah",
      "LastName": "Admin"
    },
    "Realm": "sarah-realm",
    "ClientId": "sarah-client"
  }
}
```

#### Realm Import File

The `keycloak-realm.json` file contains the complete Keycloak realm configuration:

- **Realm**: `sarah-realm`
- **Client**: `sarah-client` (public client for Angular SPA)
  - Redirect URIs: `http://localhost:4200/*`, `https://localhost:4200/*`
  - Web origins: `http://localhost:4200`, `https://localhost:4200`
  - Direct access grants enabled
  - Standard flow (authorization code) enabled
- **Test User**: Pre-configured with credentials from appsettings
- **Token Settings**: 
  - Access token: 5 minutes (300 seconds)
  - Refresh token: 30 minutes (1800 seconds)

### Customizing Credentials

You can customize the Keycloak admin and test user credentials in two ways:

1. **Edit appsettings.json** (for production/default values):
   ```json
   {
     "Keycloak": {
       "AdminUser": "your-admin-username",
       "AdminPassword": "YourStrongPassword123!",
       "TestUser": {
         "Username": "your-test-user",
         "Password": "YourTestPassword123!"
       }
     }
   }
   ```

2. **Edit appsettings.Development.json** (for development overrides):
   ```json
   {
     "Keycloak": {
       "AdminUser": "admin",
       "AdminPassword": "DevPassword123!",
       "TestUser": {
         "Username": "sarah-admin",
         "Password": "DevPassword123!"
       }
     }
   }
   ```

> **Note**: The test user credentials in `appsettings.json` should match the user credentials in `keycloak-realm.json` for the realm import to work correctly.

## Running the Application

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Node.js 20+ (for Angular frontend)

### Start All Services

```bash
# From the repository root
dotnet run --project Sarah.AppHost
```

This will:
1. Start the Aspire Dashboard at `https://localhost:15888`
2. Pull and start all required Docker containers (Keycloak, RabbitMQ, PostgreSQL)
3. Import the Keycloak realm with pre-configured settings
4. Start all microservices
5. Start the Angular frontend

### Access the Application

- **Aspire Dashboard**: https://localhost:15888
- **Angular Frontend**: http://localhost:4200
- **Keycloak Admin Console**: http://localhost:8080 or https://localhost:8443
- **RabbitMQ Management**: http://localhost:15672

### Default Credentials

#### Keycloak Admin Console
- Username: `admin` (or as configured in appsettings)
- Password: `ChangeMe123!` (or as configured in appsettings)

#### Sarah Application (Test User)
- Username: `sarah-admin` (or as configured in appsettings)
- Password: `TestPassword123!` (or as configured in appsettings)

## Service Discovery

Backend services automatically discover Keycloak through Aspire's service discovery mechanism:

- The `.WithReference(keycloak)` in Program.cs injects the Keycloak connection information
- Services read the Keycloak URL from the injected configuration
- The `OIDCAuthority` configuration in backend services uses the service discovery URL

## Troubleshooting

### Keycloak Takes Long to Start

Keycloak may take 30-60 seconds to fully start and import the realm. Check the Aspire Dashboard logs for the Keycloak container to see the import progress.

### Realm Import Not Working

If the realm import doesn't work:
1. Ensure `keycloak-realm.json` exists in the Sarah.AppHost directory
2. Check that the bind mount path is correct (relative path `./keycloak-realm.json`)
3. Verify the Keycloak container has the `--import-realm` argument
4. Check Keycloak container logs for import errors

### Services Can't Connect to Keycloak

If services fail to connect to Keycloak:
1. Ensure Keycloak is fully started (check Aspire Dashboard)
2. Verify the service has `.WithReference(keycloak)` in Program.cs
3. Check that the `OIDCAuthority` in the service's configuration matches the realm URL
4. Review service logs for SSL/TLS certificate errors (expected in development)

## Development Notes

### Persistent Data

Keycloak uses a persistent data volume, so realm and user data survives container restarts. To reset Keycloak to a clean state:

```bash
# Stop all services
docker stop $(docker ps -q)

# Remove Keycloak volume
docker volume rm sarah-apphost-keycloak-data

# Restart services
dotnet run --project Sarah.AppHost
```

### Modifying the Realm Configuration

If you need to modify the realm configuration:

1. Edit `keycloak-realm.json` with your changes
2. Remove the Keycloak data volume (see above)
3. Restart the AppHost

Alternatively, use the Keycloak Admin Console to make changes, then export the realm to update the JSON file.

## Architecture

The AppHost orchestrates services in the following dependency order:

1. **Infrastructure Services** (Keycloak, RabbitMQ, PostgreSQL)
2. **Backend Microservices** (depend on infrastructure)
3. **Frontend** (depends on backend services)

All services are registered with Aspire's service discovery, allowing them to communicate using service names instead of hardcoded URLs.
