# Sarah - Smart Home Management System

> A modern smart home management platform built with microservices architecture

## Overview

**Sarah** is an intelligent smart home management system that enables you to monitor, control, and automate your connected devices through a powerful microservices architecture. Built with .NET 10 and Angular, Sarah provides a scalable, event-driven platform for managing lights, sensors, switches, and other IoT devices with advanced features like geofencing, voice control, and automation rules.

### Key Features

✨ **Device Management** - Centralized control for all smart home devices  
🔐 **Enterprise Security** - OAuth2/OIDC authentication via Keycloak  
📍 **Geofencing** - Location-based automation and triggers  
🎤 **Voice Control** - Speech recognition and text-to-speech capabilities  
⚡ **Event-Driven** - Real-time updates via RabbitMQ message broker  
📊 **Monitoring** - System health tracking and metrics  
🤖 **Automation** - Flexible rule engine for device automation  
🏠 **Room Management** - Organize devices by room and location

## Technology Stack

| Category | Technology | Purpose |
|----------|-----------|---------|
| **Backend Framework** | .NET 10 / ASP.NET Core | Microservices runtime |
| **Frontend Framework** | Angular 18+ | Single-page application |
| **Identity & Access** | Keycloak | OAuth2/OIDC authentication |
| **Message Broker** | RabbitMQ 3.x | Event-driven communication |
| **Database** | PostgreSQL 16 | Data persistence per service |
| **ORM** | Entity Framework Core | Database access layer |
| **Containerization** | Docker & Docker Compose | Deployment |
| **API Documentation** | OpenAPI/Swagger | REST API specification |

## Architecture

### High-Level System Architecture

Sarah follows a microservices architecture with clear separation of concerns, event-driven communication, and centralized authentication.

```mermaid
graph TB
    subgraph "Client Layer"
        Angular["Angular SPA<br/>(Port 4200)<br/>Modern UI"]
    end
    
    subgraph "Identity & Access Management"
        Keycloak["Keycloak IDP<br/>(Ports 8080/8443)<br/>OAuth2/OIDC"]
    end
    
    subgraph "Microservices Layer"
        DeviceService["Device Service<br/>(Port 5001)<br/>Device Management"]
        PersonsService["Persons Service<br/>(Port 5002)<br/>User Management"]
        GeofencesService["Geofences Service<br/>(Port 5003)<br/>Location Tracking"]
        MonitoringService["Monitoring Service<br/>(Port 5005)<br/>System Monitoring"]
        RulesService["Rules Service<br/>(Port 5006)<br/>Automation Rules"]
        SpeechServer["Speech Server<br/>(Port 5011)<br/>Voice Commands"]
    end
    
    subgraph "Infrastructure Layer"
        RabbitMQ["RabbitMQ<br/>(Ports 5672/15672)<br/>Message Broker<br/>Topic Exchanges"]
        
        subgraph "PostgreSQL Databases"
            DB1["devicesdb<br/>(Port 5432)"]
            DB2["personsdb<br/>(Port 5433)"]
            DB3["geofencesdb<br/>(Port 5434)"]
            DB4["monitoringdb<br/>(Port 5436)"]
            DB5["rulesdb<br/>(Port 5437)"]
        end
    end
    
    %% Client connections
    Angular -->|HTTPS/JWT| DeviceService
    Angular -->|HTTPS/JWT| PersonsService
    Angular -->|HTTPS/JWT| GeofencesService
    Angular -->|OAuth2/OIDC Login| Keycloak
    
    %% Microservice authentication
    DeviceService -->|JWT Validation| Keycloak
    PersonsService -->|JWT Validation| Keycloak
    GeofencesService -->|JWT Validation| Keycloak
    MonitoringService -->|JWT Validation| Keycloak
    RulesService -->|JWT Validation| Keycloak
    SpeechServer -->|JWT Validation| Keycloak
    
    %% Event-driven communication (RabbitMQ)
    DeviceService -.->|Pub/Sub Events| RabbitMQ
    PersonsService -.->|Pub/Sub Events| RabbitMQ
    GeofencesService -.->|Pub/Sub Events| RabbitMQ
    MonitoringService -.->|Pub/Sub Events| RabbitMQ
    RulesService -.->|Pub/Sub Events| RabbitMQ
    SpeechServer -.->|Pub/Sub Events| RabbitMQ
    
    %% Database connections
    DeviceService -->|Read/Write| DB1
    PersonsService -->|Read/Write| DB2
    GeofencesService -->|Read/Write| DB3
    MonitoringService -->|Read/Write| DB4
    RulesService -->|Read/Write| DB5
    
    style Angular fill:#4285f4,stroke:#333,stroke-width:2px,color:#fff
    style Keycloak fill:#008aaa,stroke:#333,stroke-width:2px,color:#fff
    style RabbitMQ fill:#ff6600,stroke:#333,stroke-width:2px,color:#fff
```

### Service Communication Patterns

Sarah uses two complementary communication patterns:

```mermaid
graph LR
    subgraph "Synchronous Communication (HTTP/REST)"
        Client[Client/Service] -->|REST API Request| Service[Microservice]
        Service -->|JSON Response| Client
    end
    
    subgraph "Asynchronous Communication (RabbitMQ)"
        Publisher[Event Publisher] -->|Publish Event| Exchange[Topic Exchange]
        Exchange -->|Route by Topic| Queue1[Service Queue 1]
        Exchange -->|Route by Topic| Queue2[Service Queue 2]
        Queue1 -->|Consume| Subscriber1[Subscriber 1]
        Queue2 -->|Consume| Subscriber2[Subscriber 2]
    end
    
    style Exchange fill:#ff6600,stroke:#333,stroke-width:2px,color:#fff
```

**Synchronous (HTTP/REST)**:
- Client-to-service requests (Angular → Services)
- JWT token authentication on every request
- RESTful API endpoints with OpenAPI documentation

**Asynchronous (RabbitMQ Topic Exchanges)**:
- Device state changes and sensor readings
- Speech events (voice commands, text-to-speech)
- Person availability and location updates
- Geofence entry/exit events
- System monitoring metrics
- Automation rule triggers

### Event Flow Example

```mermaid
sequenceDiagram
    participant User
    participant Angular
    participant Keycloak
    participant DeviceService
    participant RabbitMQ
    participant RulesService
    
    User->>Angular: Turn on light
    Angular->>Keycloak: Authenticate (OAuth2)
    Keycloak-->>Angular: Access Token (JWT)
    Angular->>DeviceService: POST /devices/{id}/state (JWT)
    DeviceService->>Keycloak: Validate JWT
    Keycloak-->>DeviceService: Token Valid
    DeviceService->>DeviceService: Update device state
    DeviceService->>RabbitMQ: Publish DeviceStateChanged event
    DeviceService-->>Angular: 200 OK
    RabbitMQ->>RulesService: Consume event
    RulesService->>RulesService: Check automation rules
    RulesService->>RabbitMQ: Publish RuleTriggered (if applicable)
```

### Key Components

#### Frontend Layer
- **Angular SPA** (Port 4200): Modern single-page application
  - Built with Angular 18+
  - Secure authentication via OAuth2/OpenID Connect
  - Real-time device control and monitoring
  - Responsive design for desktop and mobile

#### Microservices Layer
- **Device Service** (Port 5001): Smart device management
  - CRUD operations for devices (lights, sensors, switches)
  - Device state management and control
  - PostgreSQL database: `devicesdb`
  
- **Persons Service** (Port 5002): User and person management
  - User profiles and preferences
  - Person tracking and availability
  - PostgreSQL database: `personsdb`
  
- **Geofences Service** (Port 5003): Location-based automation
  - Geofence definitions and boundaries
  - Location tracking and entry/exit detection
  - Automation triggers based on location
  - PostgreSQL database: `geofencesdb`
  
- **Monitoring Service** (Port 5005): System health and metrics
  - Service health checks
  - Performance metrics collection
  - System monitoring dashboards
  - PostgreSQL database: `monitoringdb`
  
- **Rules Service** (Port 5006): Automation engine
  - Rule definitions (if-then-else logic)
  - Event-driven rule execution
  - Automation workflows
  - PostgreSQL database: `rulesdb`
  
- **Speech Server** (Port 5011): Voice interaction
  - Voice command recognition
  - Text-to-speech synthesis
  - Natural language processing

#### Infrastructure Layer
- **Keycloak** (Ports 8080 HTTP / 8443 HTTPS): Identity Provider
  - OAuth2 and OpenID Connect authentication
  - JWT token issuance and validation
  - User realm: `sarah-realm`
  - Role-based access control
  
- **RabbitMQ** (Ports 5672 AMQP / 15672 Management): Message Broker
  - Topic-based exchanges for event distribution
  - Durable queues for reliability
  - Event-driven inter-service communication
  - Management UI for monitoring
  
- **PostgreSQL 16** (Multiple databases): Data Persistence
  - Separate database per microservice
  - Database-per-service pattern for data isolation
  - Entity Framework Core for ORM
  - Migration support
  - Ports: 5432-5437 (one per database)

### Security

All services implement JWT bearer token authentication validated against Keycloak:
- Every API request requires a valid access_token
- Tokens are validated against the configured Keycloak realm
- Services verify token signature, issuer, audience, and expiration
- SSL/TLS encryption for all communications in production

## Getting Started

### Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** - Required for running microservices
- **[Node.js 20+](https://nodejs.org/)** - Required for Angular frontend
- **[Docker Desktop](https://www.docker.com/)** - Required for containerized services
- **[Docker Compose](https://docs.docker.com/compose/)** - Included with Docker Desktop
- **[Git](https://git-scm.com/)** - For cloning the repository

### Three Ways to Run Sarah

Sarah can be run in three different modes depending on your needs:

| Method | Best For | Complexity | Setup Time |
|--------|----------|------------|------------|
| **Aspire (Recommended)** | Development, Modern Cloud-Native | Low | 1-2 minutes |
| **Docker Compose** | Local Testing, Traditional Docker | Low | 2-3 minutes |
| **Manual Setup** | Debugging, Fine-Grained Control | High | 5-10 minutes |

---

### Option 1: .NET Aspire (Recommended) 🚀

.NET Aspire provides the most modern development experience with automatic service orchestration, service discovery, and zero manual configuration.

#### Why Aspire?

✅ **Zero Manual Configuration** - No need to manually configure Keycloak realm, clients, or users  
✅ **Automatic Service Discovery** - Services automatically find each other  
✅ **Built-in Dashboard** - Visual monitoring of all services and their dependencies  
✅ **Reproducible Setup** - Same configuration every time, perfect for team development  
✅ **Fast Startup** - All services orchestrated efficiently

#### Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**
- **[Docker Desktop](https://www.docker.com/)** - Must be running
- **[Node.js 20+](https://nodejs.org/)** - For Angular frontend

#### Step 1: Clone the Repository

```bash
git clone https://github.com/fondencn/sarah.git
cd sarah
```

#### Step 2: (Optional) Customize Credentials

The default credentials work out of the box, but you can customize them by editing `Sarah.AppHost/appsettings.json`:

```json
{
  "Keycloak": {
    "AdminUser": "admin",
    "AdminPassword": "ChangeMe123!",
    "TestUser": {
      "Username": "sarah-admin",
      "Password": "TestPassword123!",
      "Email": "admin@sarah.local"
    }
  }
}
```

For development-specific overrides, edit `Sarah.AppHost/appsettings.Development.json`.

> **🔒 Security Note**: These are default development credentials. **Never use these passwords in production**. For production deployments, use environment variables or secure configuration providers like Azure Key Vault.

#### Step 3: Start All Services with Aspire

```bash
dotnet run --project Sarah.AppHost
```

That's it! Aspire will:
1. ✅ Start the Aspire Dashboard
2. ✅ Pull and start Docker containers (Keycloak, RabbitMQ, PostgreSQL)
3. ✅ **Automatically import Keycloak realm** with pre-configured client and test user
4. ✅ Start all microservices with automatic service discovery
5. ✅ Start the Angular frontend
6. ✅ Wire everything together

**First run**: Takes 60-90 seconds for all services to be ready

#### Step 4: Access the Application

Once started, access:

| Service | URL | Credentials |
|---------|-----|-------------|
| **Aspire Dashboard** 🎯 | https://localhost:15888 | None (dev mode) |
| **Angular Frontend** | http://localhost:4200 | `sarah-admin` / `TestPassword123!` |
| **Keycloak Admin Console** | http://localhost:8080 | `admin` / `ChangeMe123!` |
| **RabbitMQ Management** | http://localhost:15672 | `guest` / `guest` |

> 💡 **Tip**: The Aspire Dashboard shows all running services, their logs, traces, and metrics in real-time!

#### Step 5: Login to Sarah

1. Navigate to http://localhost:4200
2. You'll be redirected to Keycloak login
3. Login with:
   - **Username**: `sarah-admin`
   - **Password**: `TestPassword123!` (or your custom password from appsettings)
4. You'll be redirected back to Sarah's home page

No manual Keycloak configuration needed! Everything is pre-configured.

#### Understanding What Aspire Does

Aspire automatically configures:

- **Keycloak Realm**: `sarah-realm` is created on startup
- **Keycloak Client**: `sarah-client` configured for Angular SPA
  - Redirect URIs: `http://localhost:4200/*`, `https://localhost:4200/*`
  - Direct access grants enabled
  - Standard flow (authorization code) enabled
- **Test User**: Pre-created with credentials from appsettings
- **Token Lifetimes**: 5 min access tokens, 30 min refresh tokens
- **Service Discovery**: All services automatically discover Keycloak, RabbitMQ, and PostgreSQL

For more details on Aspire configuration, see [Sarah.AppHost/README.md](Sarah.AppHost/README.md).

#### Stopping Aspire

Press `Ctrl+C` in the terminal where Aspire is running. All services will stop gracefully.

#### Troubleshooting Aspire

**Keycloak takes a long time to start**  
- First startup: Keycloak may take 60-90 seconds to download the image and import the realm
- Subsequent startups: Should be faster (30-45 seconds)
- Check Keycloak logs in Aspire Dashboard for progress

**Services can't authenticate**  
- Ensure Keycloak is fully started (check Aspire Dashboard)
- The realm import happens automatically, but takes time
- Check Keycloak container logs for "Imported realm" message

**Port conflicts**  
- Ensure ports 4200, 8080, 8443, 5001-5006, 5672, 15672, 15888 are available
- Stop any other instances of Docker Compose or manual services

---

### Option 2: Docker Compose (Traditional)

Docker Compose provides a complete local development environment with all services pre-configured. Use this if you prefer traditional Docker tooling or don't want to use Aspire.

> ⚠️ **Note**: Docker Compose requires manual Keycloak configuration (realm, client, user creation). For automatic configuration, use Aspire instead.

#### Step 1: Clone the Repository

```bash
git clone https://github.com/fondencn/sarah.git
cd sarah
```

#### Step 2: Configure Credentials (IMPORTANT)

```bash
# Copy the example environment file
cp .env.example .env

# Edit .env with your preferred editor and set strong passwords
nano .env  # or vim, code, etc.
```

Update the following in `.env`:
```env
# Use strong, unique passwords for production!
KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=YourStrongPassword123!

RABBITMQ_USER=sarahuser
RABBITMQ_PASSWORD=YourStrongPassword456!

POSTGRES_USER=postgres
POSTGRES_PASSWORD=YourStrongPassword789!
```

> 🔒 **Security Note**: Never commit `.env` with real credentials to version control!

#### Step 3: Start All Services

```bash
docker-compose -f docker-compose.microservices.yml up --build
```

This will start:
- ✅ Keycloak (Identity Provider)
- ✅ RabbitMQ (Message Broker)
- ✅ PostgreSQL containers for devices, persons, geofences, monitoring, and rules
- ✅ 6 microservices (Device, Persons, Geofences, Monitoring, Rules, Speech)
- ✅ Angular frontend

**First run**: Takes 2-3 minutes for Keycloak to initialize

#### Step 4: Configure Keycloak (First Time Only)

1. Open Keycloak Admin Console: http://localhost:8080
2. Login with credentials from your `.env` file (default: `admin`/`admin`)
3. Create a new realm:
   - Click **"Create Realm"**
   - Name: `sarah-realm`
   - Click **"Create"**
4. Create a client:
   - Navigate to **Clients** → **"Create client"**
   - Client ID: `sarah-client`
   - Click **"Next"** → **"Save"**
5. Configure client settings:
   - **Valid redirect URIs**: `http://localhost:4200/*`
   - **Web origins**: `http://localhost:4200`
   - **Direct Access Grants**: Enable
   - Click **"Save"**

#### Step 5: Access Services

Once all services are running, access them at:

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend (Angular)** | http://localhost:4200 | (via Keycloak) |
| **Device Service** | http://localhost:5001 | JWT required |
| **Persons Service** | http://localhost:5002 | JWT required |
| **Geofences Service** | http://localhost:5003 | JWT required |
| **Monitoring Service** | http://localhost:5005 | JWT required |
| **Rules Service** | http://localhost:5006 | JWT required |
| **Speech Server** | http://localhost:5011 | JWT required |
| **Keycloak Admin** | http://localhost:8080 | From `.env` |
| **RabbitMQ Management** | http://localhost:15672 | From `.env` |
| **PostgreSQL** | localhost:5432-5437 | From `.env` |

#### Step 6: Stop Services

```bash
# Stop services but keep data
docker-compose -f docker-compose.microservices.yml stop

# Stop services and remove containers (data persists in volumes)
docker-compose -f docker-compose.microservices.yml down

# Stop services and remove all data (⚠️ destructive)
docker-compose -f docker-compose.microservices.yml down -v
```

---

### Option 3: Manual Development Setup

For fine-grained control and debugging individual services.

> ⚠️ **Note**: This method requires manual Keycloak configuration and is the most complex. Recommended only for advanced debugging scenarios.

#### Step 1: Start Infrastructure Services

```bash
docker-compose -f docker-compose.microservices.yml up keycloak rabbitmq postgres-devices postgres-persons postgres-geofences postgres-monitoring postgres-rules
```

#### Step 2: Configure Keycloak

Follow Keycloak configuration steps from Docker Compose Option (Step 4).

#### Step 3: Run Microservices Individually

Open separate terminal windows for each service:

```bash
# Terminal 1 - Device Service
cd Microservices/Sarah.DeviceService.WebApi
dotnet run

# Terminal 2 - Persons Service
cd Microservices/Sarah.Persons.WebApi
dotnet run

# Terminal 3 - Geofences Service
cd Microservices/Sarah.Geofences.WebApi
dotnet run

# Terminal 4 - Monitoring Service
cd Microservices/Sarah.Monitoring.WebApi
dotnet run

# Terminal 5 - Rules Service
cd Microservices/Sarah.Rules.WebApi
dotnet run

# Terminal 6 - Speech Server
cd Microservices/Sarah.SpeechServer.WebApi
dotnet run

# Terminal 7 - Angular Frontend
cd sarah.client
npm install
npm start
```

#### Step 4: Access Services

When running with `dotnet run`, services use ports from `launchSettings.json`. To match Docker Compose ports, use:
```bash
dotnet run --urls "http://localhost:5001"
```

## Configuration

### Service Configuration

Each microservice can be configured via `appsettings.json`, environment variables, or command-line arguments.

#### Keycloak/JWT Configuration

```json
{
  "OIDCAuthority": "http://keycloak:8080/realms/sarah-realm",
  "Jwt": {
    "Issuer": "http://keycloak:8080/realms/sarah-realm",
    "Audience": "account"
  }
}
```

> **Note**: `RequireHttpsMetadata` is automatically set based on the environment (`false` in Development, `true` in Production) and is not read from configuration.

**Environment Variables** (Docker Compose):
```bash
OIDCAuthority=http://keycloak:8080/realms/sarah-realm
Jwt__Issuer=http://keycloak:8080/realms/sarah-realm
Jwt__Audience=account
```

#### RabbitMQ Configuration

```json
{
  "RabbitMQ": {
    "HostName": "rabbitmq",
    "Port": 5672,
    "UserName": "your-username",
    "Password": "your-password"
  }
}
```

**Environment Variables**:
```bash
RabbitMQ__HostName=rabbitmq
RabbitMQ__Port=5672
RabbitMQ__UserName=sarahuser
RabbitMQ__Password=YourPassword
```

#### Database Configuration

Each service uses its own PostgreSQL database with the connection string key `PostgresConnection`:

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=postgres-devices;Database=devicesdb;Username=postgres;Password=yourpassword"
  }
}
```

**Environment Variables**:
```bash
ConnectionStrings__PostgresConnection=Host=postgres-devices;Database=devicesdb;Username=postgres;Password=yourpassword
```

### Port Mappings Reference

| Service | Internal Port | External Port | Protocol |
|---------|--------------|---------------|----------|
| Angular Frontend | 4200 | 4200 | HTTP |
| Device Service | 8080 | 5001 | HTTP |
| Persons Service | 8080 | 5002 | HTTP |
| Geofences Service | 8080 | 5003 | HTTP |
| Monitoring Service | 8080 | 5005 | HTTP |
| Rules Service | 8080 | 5006 | HTTP |
| Speech Server | 8080 | 5011 | HTTP |
| Keycloak | 8080 | 8080 | HTTP |
| Keycloak (HTTPS) | 8443 | 8443 | HTTPS |
| RabbitMQ AMQP | 5672 | 5672 | AMQP |
| RabbitMQ Management | 15672 | 15672 | HTTP |
| PostgreSQL (devices) | 5432 | 5432 | TCP |
| PostgreSQL (persons) | 5432 | 5433 | TCP |
| PostgreSQL (geofences) | 5432 | 5434 | TCP |
| PostgreSQL (monitoring) | 5432 | 5436 | TCP |
| PostgreSQL (rules) | 5432 | 5437 | TCP |

## Development

### Project Structure

```
sarah/
├── .aspire/                        # Aspire configuration
├── .github/                        # GitHub Actions workflows
├── .vscode/                        # VS Code settings
├── Libs/                           # Shared libraries
│   ├── Sarah.API/                  # Common API models
│   ├── Sarah.Authentication/       # JWT authentication library
│   ├── Sarah.DeviceServiceClient/  # Device service client
│   ├── Sarah.LEDService/           # LED device logic
│   ├── Sarah.Messaging.RabbitMQ/   # RabbitMQ messaging
│   ├── Sarah.SpeakerEventClient/   # Speaker client
│   └── Sarah.Voice/                # Voice processing
├── Microservices/                  # Microservice applications
│   ├── Sarah.DeviceService.WebApi/
│   │   ├── Controllers/            # API endpoints
│   │   ├── Data/                   # DbContext
│   │   ├── DTOs/                   # Data transfer objects
│   │   ├── Migrations/             # EF migrations
│   │   ├── Services/               # Business logic
│   │   └── Program.cs
│   ├── Sarah.Persons.WebApi/
│   ├── Sarah.Geofences.WebApi/
│   ├── Sarah.RoomService.WebApi/
│   ├── Sarah.Monitoring.WebApi/
│   ├── Sarah.Rules.WebApi/
│   └── Sarah.SpeechServer.WebApi/
├── Sarah.AppHost/                  # .NET Aspire orchestration
│   ├── Program.cs                  # Aspire configuration
│   └── Sarah.AppHost.csproj
├── sarah.client/                   # Angular frontend
│   ├── src/
│   │   ├── app/                    # Angular components
│   │   ├── assets/                 # Static assets
│   │   └── environments/           # Environment configs
│   ├── angular.json
│   └── package.json
├── docker-compose.microservices.yml # Production deployment
├── .env.example                    # Environment template
├── Sarah.sln                       # Solution file
└── README.md                       # This file
```

### Building the Project

#### Build All .NET Projects

```bash
# Restore dependencies
dotnet restore Sarah.sln

# Build solution
dotnet build Sarah.sln

# Build in Release mode
dotnet build Sarah.sln -c Release
```

#### Build Individual Services

```bash
cd Microservices/Sarah.DeviceService.WebApi
dotnet build
```

#### Build Angular Frontend

```bash
cd sarah.client

# Install dependencies
npm install

# Development build
npm run build

# Production build
npm run build -- --configuration production
```

### Running Tests

```bash
# Run all .NET tests
dotnet test Sarah.sln

# Run tests with coverage
dotnet test Sarah.sln --collect:"XPlat Code Coverage"

# Run Angular tests
cd sarah.client
npm test

# Run Angular e2e tests
npm run e2e
```

### Database Migrations

Each service manages its own database migrations with Entity Framework Core:

```bash
# Create a new migration
cd Microservices/Sarah.DeviceService.WebApi
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Generate SQL script
dotnet ef migrations script
```

### API Documentation

Each service exposes OpenAPI/Swagger documentation:

- Device Service: http://localhost:5001/swagger
- Persons Service: http://localhost:5002/swagger
- Geofences Service: http://localhost:5003/swagger
- Room Service: http://localhost:5004/swagger
- Monitoring Service: http://localhost:5005/swagger
- Rules Service: http://localhost:5006/swagger
- Speech Server: http://localhost:5008/swagger

### Code Style and Linting

```bash
# Format C# code
dotnet format Sarah.sln

# Format with verification
dotnet format Sarah.sln --verify-no-changes

# Lint Angular code
cd sarah.client
npm run lint

# Fix linting issues
npm run lint -- --fix
```

## Usage Examples

### Device Management

**Add a New Device**:
```bash
curl -X POST http://localhost:5001/api/devices \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Living Room Light",
    "type": "light",
    "roomId": "123"
  }'
```

**Control a Device**:
```bash
curl -X PUT http://localhost:5001/api/devices/{id}/state \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "state": "on", "brightness": 80 }'
```

### Automation Rules

**Create an Automation Rule**:
```bash
curl -X POST http://localhost:5006/api/rules \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Turn on lights at sunset",
    "trigger": "time",
    "condition": "sunset",
    "actions": [
      { "deviceId": "123", "action": "turn_on" }
    ]
  }'
```

### Geofencing

**Create a Geofence**:
```bash
curl -X POST http://localhost:5003/api/geofences \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Home",
    "latitude": 37.7749,
    "longitude": -122.4194,
    "radius": 100
  }'
```

### Voice Commands

Send voice commands through the Speech Server to control devices using natural language.

## Monitoring and Observability

### Health Checks

Each service exposes a status endpoint for health monitoring:

```bash
# Device service status
curl http://localhost:5001/api/devices/status

# Geofences service status
curl http://localhost:5003/api/geofences/status

# Rules service status
curl http://localhost:5006/api/rules/status
```

### RabbitMQ Management

Access the RabbitMQ management console at http://localhost:15672 to:
- Monitor message queues
- View exchange configurations
- Track message rates
- Debug message routing

### Service Logs

#### Docker Compose Logs
```bash
# View all logs
docker-compose -f docker-compose.microservices.yml logs

# Follow logs in real-time
docker-compose -f docker-compose.microservices.yml logs -f

# View specific service logs
docker-compose -f docker-compose.microservices.yml logs deviceservice

# Tail last 100 lines
docker-compose -f docker-compose.microservices.yml logs --tail=100 -f
```

#### Aspire Dashboard
When using .NET Aspire, access the dashboard for integrated logging and telemetry across all services.

## Troubleshooting

### Common Issues

**Keycloak not starting**:
- Wait 30-60 seconds on first run for initialization
- Check logs: `docker-compose logs keycloak`
- Verify port 8080 is not in use

**Services can't connect to Keycloak**:
- Ensure Keycloak realm `sarah-realm` is created
- Verify client `sarah-client` is configured
- Check JWT issuer matches realm URL

**RabbitMQ connection errors**:
- Verify RabbitMQ is healthy: `docker-compose ps`
- Check credentials in `.env` file
- Ensure port 5672 is not blocked

**Database connection errors**:
- Check PostgreSQL containers are running
- Verify connection strings in environment variables
- Run migrations: `dotnet ef database update`

**Port conflicts**:
- Check if ports are already in use: `netstat -an | grep LISTEN`
- Modify port mappings in `docker-compose.microservices.yml`
- Update service configurations accordingly

## Authentication and Authorization

Sarah uses a comprehensive OAuth2 with OpenID Connect (OIDC) authentication flow:

### Authentication Flow

```mermaid
sequenceDiagram
    participant User
    participant Angular
    participant Keycloak
    participant Service
    
    User->>Angular: Open application
    Angular->>Keycloak: Redirect to login
    Keycloak->>User: Show login page
    User->>Keycloak: Enter credentials
    Keycloak->>Angular: Return authorization code
    Angular->>Keycloak: Exchange code for tokens
    Keycloak->>Angular: Access token + Refresh token
    Angular->>Service: API request with Bearer token
    Service->>Keycloak: Validate token (JWKS)
    Keycloak->>Service: Token valid
    Service->>Angular: Return response
```

### Token Management

- **Access Tokens**: Short-lived (default: 5 minutes)
- **Refresh Tokens**: Long-lived (default: 30 minutes)
- **Token Validation**: Performed by each microservice using Keycloak JWKS endpoint
- **Token Storage**: Secure storage in Angular (HttpOnly cookies recommended)

### Authorization

Services use claims-based authorization:
- **Roles**: Define user permissions (admin, user, viewer)
- **Scopes**: Define API access levels
- **Claims**: Additional user attributes for fine-grained control

## Performance and Scalability

### Scalability Considerations

Sarah's microservices architecture enables horizontal scaling:

- **Stateless Services**: All microservices are stateless (session state in JWT)
- **Database Per Service**: Independent scaling of data layer
- **Message Queue**: RabbitMQ buffers events during high load
- **Container Orchestration**: Easy to deploy on Kubernetes or Docker Swarm

### Performance Best Practices

1. **Connection Pooling**: PostgreSQL and RabbitMQ use connection pools
2. **Caching**: Consider Redis for frequently accessed data
3. **Async Operations**: RabbitMQ for non-blocking operations
4. **Load Balancing**: Use reverse proxy (nginx, traefik) for production

## Deployment

### Production Deployment Checklist

- [ ] Use strong, unique passwords for all services (`.env`)
- [ ] Enable HTTPS/TLS for all services
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure `Jwt__RequireHttpsMetadata=true`
- [ ] Use managed databases (instead of containers)
- [ ] Set up backup and disaster recovery
- [ ] Configure monitoring and alerting
- [ ] Use reverse proxy with rate limiting
- [ ] Enable CORS only for trusted origins
- [ ] Review and harden Keycloak configuration
- [ ] Use secrets management (Azure Key Vault, AWS Secrets Manager)

### Docker Production Optimization

```yaml
# Example production docker-compose optimizations
services:
  deviceservice:
    restart: unless-stopped
    deploy:
      resources:
        limits:
          cpus: '1'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

## Contributing

Contributions are welcome! Please follow these guidelines:

### How to Contribute

1. **Fork the repository** on GitHub
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes** with clear, descriptive commits
4. **Add tests** for new functionality
5. **Ensure all tests pass**: `dotnet test && npm test`
6. **Format your code**: `dotnet format && npm run lint`
7. **Submit a pull request** with a clear description

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Follow [Angular Style Guide](https://angular.io/guide/styleguide)
- Write meaningful commit messages
- Add XML documentation for public APIs
- Include unit tests for business logic
- Update documentation for user-facing changes

### Reporting Issues

When reporting issues, please include:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant logs or error messages

## Roadmap

Future enhancements planned for Sarah:

- 🔄 **WebSocket Support**: Real-time bidirectional communication
- 📱 **Mobile App**: Native iOS and Android applications
- 🤖 **AI Integration**: Machine learning for predictive automation
- 🔌 **Plugin System**: Extensible device driver architecture
- 📊 **Advanced Analytics**: Historical data analysis and insights
- 🌐 **Multi-Tenant**: Support for multiple households
- 🔐 **2FA Support**: Two-factor authentication
- 🏠 **Kubernetes Support**: Container orchestration for scaling

## Resources

### Documentation
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Angular Documentation](https://angular.io/docs)

### Related Projects
- [.NET Microservices Architecture](https://dotnet.microsoft.com/apps/aspnet/microservices)
- [Keycloak Angular Adapter](https://www.npmjs.com/package/keycloak-angular)
- [MassTransit](https://masstransit-project.com/) - Advanced messaging patterns

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

### MIT License Summary

- ✅ **Commercial use** - Use in commercial applications
- ✅ **Modification** - Modify the source code
- ✅ **Distribution** - Distribute the software
- ✅ **Private use** - Use privately
- ⚠️ **Liability** - No warranty provided
- ⚠️ **Warranty** - Provided "as is" without warranty

## Contact and Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/fondencn/sarah/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/fondencn/sarah/discussions)
- **Author**: Chris (fondencn)

---

## Acknowledgments

Built with ❤️ using:
- [.NET](https://dotnet.microsoft.com/)
- [Angular](https://angular.io/)
- [Keycloak](https://www.keycloak.org/)
- [RabbitMQ](https://www.rabbitmq.com/)
- [PostgreSQL](https://www.postgresql.org/)
- [Docker](https://www.docker.com/)

Special thanks to the open-source community for these amazing tools!

---

## Developer Reference: Architecture Deep Dive

This section provides a comprehensive understanding of Sarah's software structure, use cases, and call chains to help developers (and AI assistants) quickly understand the system architecture in future sessions.

### System Architecture Overview

Sarah follows a **microservices architecture** with the following key characteristics:

- **Service Independence**: Each microservice owns its data and business logic
- **Event-Driven Communication**: Services communicate asynchronously via RabbitMQ
- **Centralized Authentication**: All services authenticate via Keycloak JWT tokens
- **Database Per Service**: Each microservice has its own PostgreSQL database
- **REST API Exposure**: Each service exposes REST endpoints for synchronous operations

### Core Services and Responsibilities

#### 1. Device Service (Port 5001)
**Purpose**: Manages all smart home devices and their states

**Key Responsibilities**:
- Device discovery and registration
- Device state management (on/off, brightness, temperature)
- Z-Wave network communication
- Device grouping and associations
- LED animations

**Database**: `devicesdb` - stores device configurations, states, and network topology

**Dependencies**:
- **Keycloak**: JWT authentication
- **RabbitMQ**: Publishes device state changes, listens for control commands
- **Shared Libraries**: `Sarah.API`, `Sarah.LEDService`, `Sarah.Messaging.RabbitMQ`

**Key Endpoints**:
- `GET /api/devices/lamps` - Retrieve all lamp devices
- `GET /api/devices/sensors` - Retrieve all sensors
- `GET /api/devices/{id}` - Get specific device details
- `GET /api/devices/status` - Service health check

#### 2. Persons Service (Port 5002)
**Purpose**: Manages users, persons, and their presence/availability

**Key Responsibilities**:
- User profile management
- Person tracking and availability status
- Presence detection integration
- User preferences and settings

**Database**: `personsdb` - stores user data, profiles, and availability states

**Dependencies**:
- **Keycloak**: JWT authentication
- **RabbitMQ**: Publishes person availability events, presence changes
- **PostgreSQL**: Stores person and user data

**Key Use Cases**:
- Track who is home/away for automation rules
- Manage user preferences for device control
- Provide presence context for geofencing

#### 3. Geofences Service (Port 5003)
**Purpose**: Location-based automation and boundary detection

**Key Responsibilities**:
- Geofence boundary definitions
- Location tracking and position updates
- Entry/exit event detection
- Home zone management

**Database**: `geofencesdb` - stores geofence definitions and location history

**Dependencies**:
- **Keycloak**: JWT authentication
- **RabbitMQ**: Publishes geofence entry/exit events
- **Persons Service**: Correlates location with persons

**Key Endpoints**:
- `GET /api/geofences` - List all geofences
- `GET /api/geofences/current?latitude={lat}&longitude={lon}` - Check current location
- `GET /api/geofences/home` - Get home geofence

**Use Case Flow**:
1. GPS tracker publishes location update
2. Geofences Service receives location
3. Checks if location crosses any boundaries
4. Publishes `PersonGeoFenceMessage` to RabbitMQ
5. Rules Service consumes event and triggers automation

#### 4. Rules Service (Port 5006)
**Purpose**: Automation engine and rule execution

**Key Responsibilities**:
- Rule definition and storage
- Event pattern matching
- Condition evaluation (time, location, device state)
- Action execution (trigger devices, send notifications)

**Database**: `rulesdb` - stores automation rules and execution history

**Dependencies**:
- **Keycloak**: JWT authentication
- **RabbitMQ**: Consumes all event types, publishes action commands
- **All Services**: Can trigger actions in any service

**Typical Rule Flow**:
```
Event (e.g., Geofence Exit) 
  → Rules Service evaluates conditions
  → Matches rule: "Turn off lights when leaving home"
  → Publishes command to Device Service
  → Device Service executes action
```

#### 5. Monitoring Service (Port 5005)
**Purpose**: System health and metrics collection

**Key Responsibilities**:
- Service health monitoring
- Performance metrics collection
- System diagnostics
- Logging aggregation

**Database**: `monitoringdb` - stores metrics, logs, and health status

#### 6. Speech Server (Port 5011)
**Purpose**: Voice command processing and text-to-speech

**Key Responsibilities**:
- Voice command recognition
- Natural language processing
- Text-to-speech synthesis
- Audio playback management

**Dependencies**:
- **Keycloak**: JWT authentication
- **RabbitMQ**: Publishes speech events (`SayMessage`, `StopAudioMessage`)
- **Device Service**: Controls devices via voice

**Use Case Example**:
```
User: "Turn on living room lights"
  → Speech Server processes command
  → Identifies intent: device control
  → Publishes device command to RabbitMQ
  → Device Service receives and executes
  → Speech Server confirms: "Living room lights are now on"
```

#### 7. Room Service (Port 5004)
**Purpose**: Room and space organization

**Key Responsibilities**:
- Room definitions and hierarchies
- Device-to-room assignments
- Zone management

**Database**: `roomsdb` - stores room definitions and device mappings

### Shared Libraries

#### Sarah.API
Core business objects, interfaces, and DTOs shared across all services:
- **Interfaces**: `IDeviceService`, `IRuleService`, `IGeoFenceService`
- **Business Objects**: `NetworkElement`, `AssociationGroup`, `SensorData`
- **DTOs**: Device models, request/response objects

#### Sarah.Authentication
Centralized JWT authentication configuration:
- Keycloak integration
- JWT token validation
- Environment-aware HTTPS metadata handling
- Shared HttpClient for JWKS retrieval

#### Sarah.Messaging.RabbitMQ
Event messaging infrastructure:
- **Base Classes**: `AbstractMessage`
- **Message Types**: 
  - `PersonGeoFenceMessage` - Geofence events
  - `SayMessage` - Text-to-speech requests
  - `WeatherEventMessages` - Weather updates
  - `StopAudioMessage` - Audio control

#### Sarah.DeviceServiceClient
Client library for Device Service API:
- HTTP client wrapper
- Type-safe API calls
- Used by services that need to control devices

### Communication Patterns and Call Chains

#### Pattern 1: Synchronous HTTP Requests
**Example: Frontend retrieves device list**
```
Angular Client
  → HTTP GET /api/devices/lamps (JWT token)
  → Device Service validates JWT with Keycloak
  → Device Service queries database
  → Returns device list as JSON
```

#### Pattern 2: Asynchronous Event Publishing
**Example: Device state change**
```
Device Service changes lamp state
  → Publishes DeviceStateChanged to RabbitMQ (topic exchange)
  → Multiple consumers receive event:
    - Rules Service: Checks if triggers any rules
    - Monitoring Service: Records state change
    - Frontend (via WebSocket): Updates UI
```

#### Pattern 3: Cross-Service Automation
**Example: "Turn off lights when leaving home"**
```
1. GPS Tracker updates position
2. Geofences Service detects exit from home zone
3. Publishes PersonGeoFenceMessage (event: exit, person: John)
4. Rules Service receives event
5. Evaluates rule: "If John exits home, turn off all lights"
6. Publishes device commands to RabbitMQ
7. Device Service receives commands
8. Turns off all lamp devices
9. Publishes state change events
10. Speech Server announces: "All lights turned off"
```

#### Pattern 4: Voice Command Processing
**Example: "What's the weather?"**
```
User speaks command
  → Speech Server captures audio
  → Processes natural language
  → Identifies intent: weather query
  → Publishes weather request to RabbitMQ
  → Monitoring Service responds with weather data
  → Speech Server synthesizes response
  → Plays audio: "It's 72 degrees and sunny"
```

### Key Use Cases

#### Use Case 1: Morning Automation
**Trigger**: 7:00 AM on weekdays
**Flow**:
1. Rules Service timer fires
2. Checks person availability (is anyone home?)
3. If home, publishes commands:
   - Turn on bedroom lights (30% brightness)
   - Start coffee maker
   - Announce weather forecast via Speech Server

#### Use Case 2: Security Monitoring
**Trigger**: Door sensor detects open door
**Flow**:
1. Device Service receives sensor event
2. Publishes door sensor state change
3. Rules Service checks conditions:
   - Is it after 10 PM?
   - Is alarm armed?
4. If yes, publishes alert
5. Speech Server announces: "Front door opened"
6. Monitoring Service logs security event

#### Use Case 3: Energy Saving
**Trigger**: Last person leaves home (geofence exit)
**Flow**:
1. Geofences Service detects all persons outside home zone
2. Publishes "AllPersonsAway" event
3. Rules Service triggers energy-saving mode:
   - Turn off all lights
   - Set thermostat to away mode
   - Disable unnecessary devices
4. Speech Server confirms via mobile notification

### Service Dependencies Matrix

| Service | Database | RabbitMQ | Keycloak | Other Services |
|---------|----------|----------|----------|----------------|
| Device Service | devicesdb | ✅ Pub/Sub | ✅ Auth | - |
| Persons Service | personsdb | ✅ Pub/Sub | ✅ Auth | - |
| Geofences Service | geofencesdb | ✅ Pub/Sub | ✅ Auth | Persons (data) |
| Rules Service | rulesdb | ✅ Pub/Sub | ✅ Auth | All (triggers) |
| Monitoring Service | monitoringdb | ✅ Pub/Sub | ✅ Auth | All (monitors) |
| Room Service | roomsdb | - | ✅ Auth | Device (assigns) |
| Speech Server | - | ✅ Pub/Sub | ✅ Auth | Device (controls) |

### Data Flow Diagram

```
┌──────────────┐
│ Angular SPA  │
└──────┬───────┘
       │ HTTP/JWT
       ▼
┌──────────────────────────────────────────┐
│         Microservices Layer              │
│  ┌────────┐ ┌────────┐ ┌──────────┐    │
│  │ Device │ │ Persons│ │ Geofences│    │
│  │ Service│ │ Service│ │  Service │    │
│  └────┬───┘ └───┬────┘ └────┬─────┘    │
│       │         │            │           │
│  ┌────┴─────────┴────────────┴─────┐    │
│  │       RabbitMQ (Events)         │    │
│  └────┬─────────┬────────────┬─────┘    │
│       │         │            │           │
│  ┌────▼───┐ ┌──▼─────┐ ┌───▼──────┐    │
│  │ Rules  │ │Monitor │ │  Speech  │    │
│  │ Service│ │ Service│ │  Server  │    │
│  └────────┘ └────────┘ └──────────┘    │
└──────────────────────────────────────────┘
       │
       ▼
┌──────────────────┐
│    Keycloak      │
│  (Auth/JWT)      │
└──────────────────┘
```

### Development Tips

1. **Adding a New Service**:
   - Create in `Microservices/` folder
   - Reference `Sarah.API` and `Sarah.Authentication` libraries
   - Add database configuration with `PostgresConnection`
   - Register in `Sarah.AppHost/Program.cs` for Aspire
   - Add to `docker-compose.microservices.yml`

2. **Adding New Event Types**:
   - Define message class in `Sarah.Messaging.RabbitMQ/Messages/`
   - Inherit from `AbstractMessage`
   - Publisher: Inject RabbitMQ client, publish to topic exchange
   - Consumer: Subscribe to relevant topics, implement handler

3. **Debugging Service Communication**:
   - Check RabbitMQ Management UI (port 15672) for message flow
   - Verify JWT tokens are valid (check Keycloak logs)
   - Use service status endpoints: `/api/{service}/status`
   - Review service logs for authentication errors

4. **Common Integration Points**:
   - Device control: Publish commands to RabbitMQ or call Device Service API
   - User context: Query Persons Service for availability
   - Location awareness: Use Geofences Service for position checks
   - Automation: Rules Service automatically triggers actions based on events

### Testing Strategy

- **Unit Tests**: Test individual service logic in isolation
- **Integration Tests**: Test service-to-service communication via RabbitMQ
- **API Tests**: Test REST endpoints with valid JWT tokens
- **End-to-End Tests**: Test complete use case flows (UI → Services → Events → Actions)

---

**Star ⭐ this repository if you find it helpful!**
