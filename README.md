# Sarah - Smart Home Management System

Chris's Smart Home with Microservice Architecture

## Architecture Overview

Sarah is a modern smart home management system built on a microservice architecture using .NET 9, Angular, Keycloak for identity management, and RabbitMQ for event-driven communication.

### Architecture Diagram

```mermaid
graph TB
    subgraph "Frontend"
        Angular[Angular SPA]
    end
    
    subgraph "Identity & Access"
        Keycloak[Keycloak IDP<br/>OpenID Connect]
    end
    
    subgraph "API Gateway"
        Gateway[API Gateway<br/>Sarah.API.WebApi]
    end
    
    subgraph "Microservices"
        DeviceService[Device Service<br/>Device Management]
        PersonsService[Persons Service<br/>User Management]
        GeofencesService[Geofences Service<br/>Location Tracking]
        EventService[Event Processing<br/>Event Handling]
        MonitoringService[Monitoring Service<br/>System Monitoring]
        RulesService[Rules Service<br/>Automation Rules]
        LocationServer[Location Server<br/>Location Services]
        SpeechServer[Speech Server<br/>Voice Commands]
    end
    
    subgraph "Infrastructure"
        RabbitMQ[RabbitMQ<br/>Message Broker<br/>Topic Exchanges]
        SQLite[(SQLite DB)]
    end
    
    Angular -->|HTTPS/JWT| Gateway
    Angular -->|OAuth2/OIDC| Keycloak
    
    Gateway -->|JWT Auth| Keycloak
    Gateway -->|HTTP| DeviceService
    Gateway -->|HTTP| PersonsService
    Gateway -->|HTTP| GeofencesService
    Gateway -->|HTTP| EventService
    Gateway -->|HTTP| MonitoringService
    Gateway -->|HTTP| RulesService
    
    DeviceService -->|JWT Validation| Keycloak
    PersonsService -->|JWT Validation| Keycloak
    GeofencesService -->|JWT Validation| Keycloak
    EventService -->|JWT Validation| Keycloak
    MonitoringService -->|JWT Validation| Keycloak
    RulesService -->|JWT Validation| Keycloak
    LocationServer -->|JWT Validation| Keycloak
    SpeechServer -->|JWT Validation| Keycloak
    
    DeviceService -.->|Pub/Sub| RabbitMQ
    PersonsService -.->|Pub/Sub| RabbitMQ
    GeofencesService -.->|Pub/Sub| RabbitMQ
    EventService -.->|Pub/Sub| RabbitMQ
    MonitoringService -.->|Pub/Sub| RabbitMQ
    RulesService -.->|Pub/Sub| RabbitMQ
    SpeechServer -.->|Pub/Sub| RabbitMQ
    
    DeviceService -->|Read/Write| SQLite
    PersonsService -->|Read/Write| SQLite
    GeofencesService -->|Read/Write| SQLite
```

### Key Components

#### Frontend Layer
- **Angular SPA**: Modern single-page application providing the user interface
  - Built with Angular 18+
  - Secure authentication via OAuth2/OpenID Connect
  - Real-time updates through WebSockets

#### API Gateway
- **Sarah.API.WebApi**: Central entry point for all client requests
  - Request routing to appropriate microservices
  - JWT token validation
  - Cross-cutting concerns (logging, monitoring)

#### Microservices Layer
- **Device Service**: Manages smart home devices (lights, sensors, switches)
- **Persons Service**: User and person management
- **Geofences Service**: Location-based automation and tracking
- **Event Processing Service**: Handles system events and notifications
- **Monitoring Service**: System health and metrics
- **Rules Service**: Automation rules and triggers
- **Location Server**: GPS and location services
- **Speech Server**: Voice command processing and text-to-speech

#### Infrastructure Layer
- **Keycloak**: Identity Provider (IDP)
  - OAuth2 and OpenID Connect authentication
  - JWT token management
  - User realm: `sarah-realm`
- **RabbitMQ**: Message Broker
  - Topic-based exchanges for event distribution
  - Asynchronous inter-service communication
  - Event-driven architecture
- **SQLite**: Data persistence
  - Lightweight embedded database
  - Device configurations and state
  - User preferences and history

### Security

All services implement JWT bearer token authentication validated against Keycloak:
- Every API request requires a valid access_token
- Tokens are validated against the configured Keycloak realm
- Services verify token signature, issuer, audience, and expiration
- SSL/TLS encryption for all communications in production

#### Security Best Practices Implemented

**✓ Shared Authentication Library**: 
- Centralized JWT authentication configuration (`Sarah.Authentication` library)
- Eliminates code duplication across microservices
- Consistent security policy enforcement

**✓ Resource Management**: 
- Shared HttpClient instance prevents socket exhaustion
- Proper resource lifecycle management

**✓ Environment-Aware Security**:
- SSL certificate validation enabled in production
- HTTPS metadata validation enforced in non-development environments
- Self-signed certificates supported only in development

**✓ Credential Management**:
- Environment variables for sensitive credentials
- `.env` file support with `.env.example` template
- No hardcoded passwords in docker-compose files
- `.gitignore` configured to prevent credential leaks

**⚠️ Known Limitations**:
- IssuerSigningKeyResolver uses `.Result` (synchronous blocking) due to framework limitations
- Signing keys are cached by JWT middleware, minimizing performance impact

### Communication Patterns

1. **Synchronous**: REST APIs between Gateway and Microservices
2. **Asynchronous**: RabbitMQ topic exchanges for events
   - Network events (sensor readings, device states)
   - Speech events (voice commands, TTS)
   - Person availability events
   - Geofence events
   - Weather and environmental events

## Technologies Used

- **Backend**: ASP.NET Core 9.0
- **Frontend**: Angular 18+
- **Identity**: Keycloak (OAuth2/OIDC)
- **Messaging**: RabbitMQ 3.x with topic exchanges
- **Database**: Entity Framework Core with SQLite
- **Orchestration**: .NET Aspire 9.1
- **Containerization**: Docker & Docker Compose

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/)

### Running with Docker Compose (Recommended)

Docker Compose provides the easiest way to run the entire Sarah ecosystem with all microservices, Keycloak, and RabbitMQ.

1. Clone the repository:
   ```bash
   git clone https://github.com/fondencn/sarah.git
   cd sarah
   ```

2. **Configure credentials** (IMPORTANT for security):
   ```bash
   # Copy the example environment file
   cp .env.example .env
   
   # Edit .env and set strong passwords for:
   # - KEYCLOAK_ADMIN_PASSWORD
   # - RABBITMQ_PASSWORD
   
   # Example (use your own secure passwords):
   # KEYCLOAK_ADMIN_PASSWORD=YourSecurePassword123!
   # RABBITMQ_PASSWORD=YourSecurePassword456!
   ```

3. Start all services:
   ```bash
   docker-compose -f docker-compose.microservices.yml up --build
   ```

4. Wait for all services to start (Keycloak takes ~30 seconds on first run)

5. Configure Keycloak (first time only):
   - Open Keycloak Admin Console: http://localhost:8080
   - Login with credentials from your `.env` file (default: admin/admin if not changed)
   - Create a new realm named `sarah-realm`
   - Create a client named `sarah-client`
   - Set redirect URIs to `http://localhost:4200/*`
   - Enable Direct Access Grants

6. Access the services:
   - **Frontend**: http://localhost:4200
   - **API Gateway**: http://localhost:5000
   - **Device Service**: http://localhost:5001
   - **Persons Service**: http://localhost:5002
   - **Geofences Service**: http://localhost:5003
   - **Event Processing Service**: http://localhost:5004
   - **Monitoring Service**: http://localhost:5005
   - **Rules Service**: http://localhost:5006
   - **Location Server**: http://localhost:5010
   - **Speech Server**: http://localhost:5011
   - **Keycloak Admin**: http://localhost:8080 (admin/admin)
   - **RabbitMQ Management**: http://localhost:15672 (guest/guest)

6. Stop all services:
   ```bash
   docker-compose -f docker-compose.microservices.yml down
   ```

### Running with .NET Aspire (Experimental)

> **Note**: .NET Aspire workload has been deprecated in favor of NuGet packages. The Aspire AppHost is included for orchestration of infrastructure services (Keycloak, RabbitMQ) but full service integration is in progress.

1. Run the AppHost:
   ```bash
   cd Sarah.AppHost
   dotnet run
   ```

2. The Aspire dashboard will start and launch Keycloak and RabbitMQ containers

3. Services can then be run individually against these infrastructure components

### Manual Setup (Development)

For development, you may want to run services individually:

1. Start infrastructure services:
   ```bash
   docker-compose -f docker-compose.microservices.yml up keycloak rabbitmq
   ```

2. Configure Keycloak as described above

3. Run microservices individually:
   ```bash
   # Each in a separate terminal
   cd Microservices/Sarah.DeviceService.WebApi && dotnet run
   cd Microservices/Sarah.Persons.WebApi && dotnet run
   cd Microservices/Sarah.API.WebApi && dotnet run
   # ... repeat for other services as needed
   ```

4. Run frontend:
   ```bash
   cd sarah.client
   npm install
   npm start
   ```

5. Access frontend at http://localhost:4200

## Configuration

### Keycloak Configuration

Configure the following in `appsettings.json` or environment variables:

```json
{
  "OIDCAuthority": "https://your-keycloak-server:8443/realms/sarah-realm",
  "Jwt": {
    "Issuer": "https://your-keycloak-server:8443/realms/sarah-realm",
    "Audience": "account"
  }
}
```

### RabbitMQ Configuration

```json
{
  "RabbitMQ": {
    "HostName": "rabbitmq-server",
    "Port": 5672,
    "UserName": "your-username",
    "Password": "your-password"
  }
}
```

## Development

### Project Structure

```
sarah/
├── Microservices/              # Microservice WebAPI projects
│   ├── Sarah.API.WebApi/       # API Gateway
│   ├── Sarah.DeviceService.WebApi/
│   ├── Sarah.Persons.WebApi/
│   ├── Sarah.Geofences.WebApi/
│   ├── Sarah.Monitoring.WebApi/
│   └── Sarah.Rules.WebApi/
├── Services/                   # Shared service libraries
│   ├── Sarah.API/              # Common API models
│   ├── Sarah.DeviceService/    # Device logic
│   ├── Sarah.Data/             # Data access
│   └── ...
├── Sarah.AppHost/              # .NET Aspire orchestration
├── Sarah.LocationServer/       # Location service
├── Sarah.SpeechServer/         # Speech service
├── sarah.client/               # Angular frontend
└── docker-compose.microservices.yml
```

### Building

```bash
# Build all projects
dotnet build Sarah.sln

# Build frontend
cd sarah.client
npm run build
```

### Testing

```bash
# Run backend tests
dotnet test

# Run frontend tests
cd sarah.client
npm test
```

## Usage

- **Dashboard**: View and manage your smart home devices
- **Device Management**: Add, edit, and control devices
- **User Authentication**: Secure login via Keycloak
- **Automation**: Create rules for automated device control
- **Voice Control**: Use speech commands to control devices
- **Location Services**: Geofence-based automation

## Authentication and Authorization

Sarah uses OAuth2 with OpenID Connect (OIDC) for authentication and authorization:

- **Keycloak** serves as the Identity Provider
- All services validate JWT access tokens
- Single Sign-On (SSO) across all services
- Fine-grained authorization through Keycloak roles
- Secure token-based API access

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For questions or inquiries, please contact Chris via his GitHub profile.
