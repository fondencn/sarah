# Target Infrastructure

This document describes the physical deployment targets for the Sarah Smart Home system.

## Hosts

| Host | Hardware | OS | Role |
|------|----------|----|------|
| **pi** | Raspberry Pi | Raspberry Pi OS (x64) | Main server — runs all infrastructure and services except SpeechServer |
| **speaker1** | Raspberry Pi | Raspberry Pi OS (x64) | Speech satellite — runs SpeechServer only |
| **speaker3** | Raspberry Pi | Raspberry Pi OS (x64) | Speech satellite — runs SpeechServer only |

All hosts are reachable via SSH from the build machine and can resolve each other by hostname.

## Main Host: `pi`

Runs the complete Sarah stack:

**Infrastructure:**
- Keycloak (OIDC identity provider) — port 8080
- RabbitMQ (message broker) — ports 5672 (AMQP), 15672 (management UI)
- PostgreSQL 16 (single instance, 6 databases) — port 5432

**Microservices:**
- DeviceService — port 5001
- PersonsService — port 5002
- GeofencesService — port 5003
- RoomService — port 5004
- MonitoringService — port 5005
- RulesService — port 5006
- DashboardService — port 5007

**Frontend:**
- Angular app served via nginx — port 80

**Hardware access:**
- Z-Wave USB stick at `/dev/ttyACM0` (passed through to DeviceService container)

## Speaker Satellites: `speaker1`, `speaker3`

Each speaker runs only:

- **SpeechServer** — port 5008

Connected to the main host for:
- RabbitMQ at `pi:5672`
- Keycloak at `pi:8080`
- DeviceService at `pi:5001`

## Network Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Home Network                             │
│                                                                 │
│  ┌──────────────────────────────────────┐                       │
│  │  pi (Main Host)                      │                       │
│  │                                      │                       │
│  │  Keycloak         :8080              │                       │
│  │  RabbitMQ         :5672 / :15672     │                       │
│  │  PostgreSQL       :5432              │                       │
│  │                                      │                       │
│  │  DeviceService    :5001  ←──────┐    │                       │
│  │  PersonsService   :5002         │    │                       │
│  │  GeofencesService :5003         │    │                       │
│  │  RoomService      :5004         │    │                       │
│  │  MonitoringService:5005         │    │                       │
│  │  RulesService     :5006         │    │                       │
│  │  DashboardService :5007         │    │                       │
│  │  Frontend (nginx) :80           │    │                       │
│  └──────────────────────────────────┘   │                       │
│           ▲          ▲          ▲        │                       │
│           │          │          │        │                       │
│  ┌────────┴───┐  ┌───┴────────┐         │                       │
│  │ speaker1   │  │ speaker3   │         │                       │
│  │            │  │            │         │                       │
│  │ Speech     │  │ Speech     │         │                       │
│  │ Server     │  │ Server     │         │                       │
│  │ :5008      │  │ :5008      │         │                       │
│  └────────────┘  └────────────┘         │                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Prerequisites per Host

All hosts require:
- Docker Engine (installed via `curl -fsSL https://get.docker.com | sh`)
- Docker Compose v2 plugin (`sudo apt-get install docker-compose-plugin`)
- Current user in the `docker` group (`sudo usermod -aG docker $USER`)
- SSH key-based authentication from the build machine
