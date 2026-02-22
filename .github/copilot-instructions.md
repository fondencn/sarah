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

**Infrastructure** (containers managed by Aspire):
- **Keycloak** `:8080` – OIDC/OAuth2 identity provider; realm imported from `Sarah.AppHost/sarah-realm-realm.json`
- **RabbitMQ** – async messaging via topic exchanges
- **PostgreSQL** – single server, one database per service (`devicesdb`, `personsdb`, `monitoringdb`, `rulesdb`, `roomsdb`, `dashboarddb`)

**Microservices** (all under `Microservices/`):

| Service | Port | Purpose |
|---------|------|---------|
| Sarah.DeviceService.WebApi | 5001 | Z-Wave & LoRaWAN device control (lamps, sensors, thermostats) |
| Sarah.Persons.WebApi | 5002 | Person/presence tracking, home network integration |
| Sarah.Geofences.WebApi | 5003 | Location-based geofence automation |
| Sarah.RoomService.WebApi | 5004 | Room management, device-to-room assignment |
| Sarah.Monitoring.WebApi | 5005 | Weather monitoring, system metrics |
| Sarah.Rules.WebApi | 5006 | Condition-based automation rules, email notifications |
| Sarah.Dashboard.WebApi | 5007 | Dashboard widget persistence |
| Sarah.SpeechServer.WebApi | 5008 | Voice recognition & TTS |

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
`WithReference(resource)` in AppHost injects `services__<name>__http__0` (and `https__0`) as environment variables into referencing projects. **Do not hardcode hostnames** like `keycloak:8080` or `rabbitmq` — read from config and let Aspire override it. Fallback chain used in this codebase:
```
services__<name>__http__0  →  explicit appsettings value  →  docker hostname fallback
```

### CORS
Configured centrally in `Sarah.ServiceDefaults`. Default policy allows any `localhost` or `127.0.0.1` origin (any port). Override per environment by adding `"AllowedOrigins": ["https://prod.example.com"]` to `appsettings.Production.json`.

### Authentication
- `AddKeycloakAuthentication()` resolves Keycloak base URL from `services__keycloak__http__0`, then `OIDCAuthority` config, then the docker hostname fallback
- Realm defaults to `"sarah-realm"`; override with `"Keycloak": { "Realm": "other-realm" }` in appsettings
- JWT audience defaults to `"account"`; override with `"Jwt": { "Audience": "..." }`
- All controllers use `[Authorize]`

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
