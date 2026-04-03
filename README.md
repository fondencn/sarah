# Sarah – Smart Home Management System

> A modern smart home platform built with .NET 10 microservices and Angular 18.

![Login screen](screenshots/01-home-login-screen.png)

## Features

✨ **Device Management** – Centralised control for lights, sensors, switches, and other IoT devices  
🏠 **Room Management** – Organise devices by room and location  
📍 **Geofencing** – Location-based automation and presence detection  
🤖 **Automation Rules** – Flexible condition-based rule engine (if-then-else) with email notifications  
📊 **Monitoring** – Weather data, system health tracking, and metrics  
🎤 **Voice Control** – Speech recognition and text-to-speech  
⚡ **Event-Driven** – Asynchronous communication via RabbitMQ topic exchanges  
🔐 **Authentication** – OAuth2/OIDC via Keycloak (configured OIDC provider)

## Component Diagram

```mermaid
graph TB
    subgraph "Client"
        Angular["Angular SPA"]
    end

    subgraph "Microservices"
        DeviceService["Device Service"]
        PersonsService["Persons Service"]
        GeofencesService["Geofences Service"]
        RoomService["Room Service"]
        MonitoringService["Monitoring Service"]
        RulesService["Rules Service"]
        DashboardService["Dashboard Service"]
        SpeechServer["Speech Server"]
    end

    subgraph "Infrastructure"
        Keycloak["Keycloak\n(Configured OIDC Provider)"]
        RabbitMQ["RabbitMQ\n(Message Broker)"]
        PostgreSQL["PostgreSQL\n(Per-Service Databases)"]
    end

    Angular -->|REST/JWT| DeviceService
    Angular -->|REST/JWT| PersonsService
    Angular -->|REST/JWT| GeofencesService
    Angular -->|REST/JWT| RoomService
    Angular -->|REST/JWT| MonitoringService
    Angular -->|REST/JWT| RulesService
    Angular -->|REST/JWT| DashboardService

    DeviceService & PersonsService & GeofencesService & RoomService & MonitoringService & RulesService & DashboardService & SpeechServer -.->|Events| RabbitMQ
    DeviceService & PersonsService & GeofencesService & RoomService & MonitoringService & RulesService & DashboardService -->|Reads/Writes| PostgreSQL

    Angular -->|"Login / token request"| Keycloak
    Keycloak -. "JWKS (token validation)" .-> DeviceService & PersonsService & GeofencesService & RoomService & MonitoringService & RulesService & DashboardService & SpeechServer

    style Keycloak fill:#008aaa,stroke:#333,stroke-width:2px,color:#fff
    style RabbitMQ fill:#ff6600,stroke:#333,stroke-width:2px,color:#fff
    style Angular fill:#4285f4,stroke:#333,stroke-width:2px,color:#fff
```

## Getting Started

Run Sarah with .NET Aspire – no manual configuration required:

```bash
git clone https://github.com/fondencn/sarah.git
cd sarah
dotnet run --project Sarah.AppHost
```

See **[docs/aspire-setup.md](docs/aspire-setup.md)** for the full setup guide including credentials, access URLs, and troubleshooting.

## Documentation

- **[Service Reference](docs/services.md)** – Details on every microservice: responsibilities, endpoints, databases, and dependencies
- **[Aspire Setup Guide](docs/aspire-setup.md)** – Full guide for running Sarah with .NET Aspire
- **[Deployment Guide](docs/deployment.md)** – Raspberry Pi deployment workflow, current deployed state, and speaker-specific operational notes
- **[AppHost README](Sarah.AppHost/README.md)** – AppHost configuration reference

For the currently deployed Raspberry Pi setup, including the temporary `speaker3` voice-recognition workaround, start with the `Current Deployed State` section in [docs/deployment.md](docs/deployment.md).

## Contributing

1. Fork the repository and create a feature branch
2. Make your changes with clear, descriptive commits
3. Ensure tests pass: `dotnet test && cd sarah.client && npm test`
4. Format code: `dotnet format && cd sarah.client && npm run lint`
5. Submit a pull request with a clear description

## License

MIT License – see [LICENSE](LICENSE) for details.
