# Sarah Smart Home – Copilot Instructions

## Build & Test Commands

```bash
# Build entire solution
dotnet build

# Build a single project
dotnet build Microservices/Sarah.Dashboard.WebApi/

# Run integration tests
dotnet test tests/Sarah.AppHost.Tests/

# Run a single test
dotnet test tests/Sarah.AppHost.Tests/ --filter "FullyQualifiedName~AppHostBuildsSuccessfully"

# Frontend dev server (Aspire injects env vars automatically)
cd sarah.client && npm start

# Frontend unit tests
cd sarah.client && npm test

# Regenerate all OpenAPI TypeScript clients from running services
cd sarah.client && npm run update-openapi

# Regenerate client for a single service
cd sarah.client && node update-openapi-clients.js --service dashboard
```

## Architecture

Sarah is a .NET Aspire-orchestrated smart home system. **Sarah.AppHost** is the entry point that starts all infrastructure and services.

## Project Structure (Current)

```text
Sarah.sln
Sarah.AppHost/                    # Aspire orchestration root
Microservices/
    Sarah.DeviceService.WebApi/     # Device control + network event processing
    Sarah.Persons.WebApi/           # Person/presence management
    Sarah.Geofences.WebApi/         # Geofence APIs
    Sarah.RoomService.WebApi/       # Room CRUD + assignments
    Sarah.Monitoring.WebApi/        # Weather/system monitoring
    Sarah.Rules.WebApi/             # Automation engine
    Sarah.Dashboard.WebApi/         # Dashboard persistence/aggregation
    Sarah.SpeechServer.WebApi/      # Speech input/output + voice actions
Libs/
    Sarah.API/                      # Shared DTOs and interfaces
    Sarah.Authentication/           # Keycloak auth extensions
    Sarah.Messaging.RabbitMQ/       # RabbitMQ client + message contracts
    Sarah.ServiceClients/           # Typed HTTP clients for inter-service calls
    Sarah.ServiceDefaults/          # Aspire defaults (discovery, OTEL, CORS)
    Sarah.LEDService/               # LED hardware support
    Sarah.Voice/                    # Voice abstractions/providers
sarah.client/                     # Angular frontend
tests/
    Sarah.AppHost.Tests/
```

**Infrastructure** (containers managed by Aspire):
- **Keycloak** `:8080` – OIDC/OAuth2 identity provider; realm imported from `Sarah.AppHost/sarah-realm-realm.json`
- **RabbitMQ** – async messaging via topic exchanges
- **PostgreSQL** – single server, one database per service (`devicesdb`, `personsdb`, `monitoringdb`, `rulesdb`, `roomsdb`, `dashboarddb`)

**Microservices** (all under `Microservices/`):

| Service | Port | Purpose | RabbitMQ in code | Direct HTTP client usage in code |
|---------|------|---------|------------------|----------------------------------|
| Sarah.DeviceService.WebApi | 5001 | Z-Wave & LoRaWAN device control (lamps, sensors, thermostats) | Yes | No typed outbound inter-service client |
| Sarah.Persons.WebApi | 5002 | Person/presence tracking, home network integration | No (even though AppHost references RabbitMQ) | Yes: DeviceService, Geofences |
| Sarah.Geofences.WebApi | 5003 | Location-based geofence automation | No (even though AppHost references RabbitMQ) | No |
| Sarah.RoomService.WebApi | 5004 | Room management, device-to-room assignment | No | No |
| Sarah.Monitoring.WebApi | 5005 | Weather monitoring, system metrics | Yes | Yes: DeviceService |
| Sarah.Rules.WebApi | 5006 | Condition-based automation rules, email notifications | Yes | Yes: DeviceService, Persons |
| Sarah.Dashboard.WebApi | 5007 | Dashboard widget persistence | No | Yes: DeviceService, Persons |
| Sarah.SpeechServer.WebApi | 5008 | Voice recognition & TTS | Yes | Yes: DeviceService |

### Inter-service Communication Map

- **RabbitMQ-backed services (active in code):** DeviceService, Monitoring, Rules, SpeechServer
- **Direct HTTP clients via `Sarah.ServiceClients` (active in code):**
    - Persons -> DeviceService, Geofences
    - Monitoring -> DeviceService
    - Rules -> DeviceService, Persons
    - Dashboard -> DeviceService, Persons
    - SpeechServer -> DeviceService
- **No active RabbitMQ client in code:** Persons, Geofences, RoomService, Dashboard

Note: `Sarah.AppHost/Program.cs` wires RabbitMQ into DeviceService, Geofences, Persons, Monitoring, Rules, and SpeechServer using `.WithReference(rabbitmq)`, but only the services listed above as RabbitMQ-backed currently register/use `RabbitMQClient` in their code.

**Shared Libraries** (all under `Libs/`):
- **Sarah.ServiceDefaults** – applied to every service via `builder.AddServiceDefaults()` / `app.UseServiceDefaults()`; registers OpenTelemetry, service discovery, and CORS
- **Sarah.Authentication** – `AddKeycloakAuthentication()` extension; reads Keycloak URL from Aspire service discovery
- **Sarah.API** – shared DTOs and service interfaces
- **Sarah.Messaging.RabbitMQ** – `RabbitMQClient` with `AbstractMessage` base and `MessageTopics` constants
- **Sarah.ServiceClients** – typed HTTP client wrappers for inter-service calls (`DeviceServiceClient`, `PersonServiceClient`, etc.)

**Frontend** (`sarah.client/` – Angular 18):
- Communicates with all services via typed API clients in `src/app/services/api-client/` (auto-generated from OpenAPI)
- Service base URLs are injected at build time by `replace-env-vars.js` (reads Aspire `services__*` env vars into `environment.ts`)

## Key Conventions

### Every new microservice must
1. Call `builder.AddServiceDefaults()` early in `Program.cs`
2. Call `app.UseServiceDefaults()` **before** `app.UseAuthentication()` (sets up CORS)
3. Call `builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment)`
4. Apply EF Core migrations at startup in the startup block pattern already used in all services
5. Be registered in `Sarah.AppHost/Program.cs` with `.WithReference(keycloak)` and any other dependencies

### Aspire service discovery
Service discovery is implemented through Aspire resource references in `Sarah.AppHost/Program.cs`.

- `WithReference(resource)` injects environment variables for referenced services into consumers.
- For HTTP endpoints, services typically consume:
    - `services__<name>__http__0`
    - `services__<name>__http-api__0` (when endpoint name is `http-api`)
- For infrastructure (Keycloak, RabbitMQ), references also inject connection settings into dependent services.

**Do not hardcode hostnames** like `keycloak:8080` or `rabbitmq` — read from config and let Aspire override it.

Fallback chain used in this codebase for service base URLs:
```
services__<name>__http__0  ->  services__<name>__http-api__0  ->  explicit appsettings value  ->  docker hostname fallback
```

### CORS
Configured centrally in `Sarah.ServiceDefaults`. Default policy allows any `localhost` or `127.0.0.1` origin (any port). Override per environment by adding `"AllowedOrigins": ["https://prod.example.com"]` to `appsettings.Production.json`.

### Authentication
- `AddKeycloakAuthentication()` resolves Keycloak base URL from `services__keycloak__http__0`, then `OIDCAuthority` config, then the docker hostname fallback
- Realm defaults to `"sarah-realm"`; override with `"Keycloak": { "Realm": "other-realm" }` in appsettings
- JWT audience defaults to `"account"`; override with `"Jwt": { "Audience": "..." }`
- All controllers use `[Authorize]`
- For inter-service HTTP calls created via `AddHttpClient(...)` and backed by `Sarah.ServiceClients`, always chain `.AddBearerTokenForwarding()` so the incoming user bearer token is propagated to downstream services.

### Repository pattern
All data access uses the generic `Repository<T>` from `Libs/` (implements `IRepository<T>`). Register per entity:
```csharp
services.AddScoped<IRepository<MyEntity>, Repository<MyEntity>>(sp =>
    new Repository<MyEntity>(sp.GetRequiredService<MyDbContext>()));
```

### RabbitMQ messaging
Messages inherit `AbstractMessage`. Topics are defined as constants in `MessageTopics`. Services publish via `RabbitMQClient.PublishAsync<T>()` and subscribe via `IHostedService` background services.

### OpenAPI clients
Frontend TypeScript clients are **generated** — never edit files under `sarah.client/src/app/services/api-client/` by hand. After changing a backend API, run `npm run update-openapi` (requires services running on their default ports).

### Database migrations
EF Core migrations run automatically on startup. To add a migration:
```bash
dotnet ef migrations add <MigrationName> --project Microservices/Sarah.<Service>.WebApi/
```
