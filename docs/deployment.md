# Deployment Guide

This guide covers building, transferring, and deploying the Sarah Smart Home system to the target Raspberry Pi infrastructure described in [infrastructure.md](infrastructure.md).

## Overview

The deployment uses Docker containers orchestrated with Docker Compose. Images are built on the development machine and transferred to the targets via SSH (`docker save` / `docker load`).

```
Build machine ── docker buildx ──► docker save ── ssh ──► docker load ── docker compose up
```

## Directory Layout

```
deploy/
├── build-and-push.sh          # Build all images & transfer to targets
├── deploy-all.sh              # Full deployment orchestrator
├── deploy-pi.sh               # Deploy main stack to pi
├── deploy-speakers.sh         # Deploy SpeechServer to speakers
├── pi/
│   ├── docker-compose.yml     # Main host compose definition
│   ├── init-databases.sh      # PostgreSQL multi-database init script
│   └── .env.example           # Environment template
└── speaker/
    ├── docker-compose.yml     # Speaker satellite compose definition
    └── .env.example           # Environment template
```

## Quick Start

### 1. Configure Environment

```bash
# Main host
cp deploy/pi/.env.example deploy/pi/.env
# Edit deploy/pi/.env — set all CHANGE_ME passwords

# Speakers
cp deploy/speaker/.env.example deploy/speaker/.env
# Edit deploy/speaker/.env — set RabbitMQ password (must match pi)
```

### 2. Full Deployment (one command)

```bash
cd deploy
./deploy-all.sh
```

This will:
1. Build all Docker images for `linux/amd64`
2. Transfer images to `pi`, `speaker1`, and `speaker3` via SSH
3. Run pre-flight checks (Docker version, disk space, connectivity)
4. Ask for confirmation before each deployment step
5. Deploy the main stack to `pi`
6. Deploy SpeechServer to `speaker1` and `speaker3`

### 3. Or Deploy Step-by-Step

```bash
# Build and transfer images
./build-and-push.sh

# Deploy main host
./deploy-pi.sh

# Deploy speakers
./deploy-speakers.sh
```

## Pre-flight Checks

Every deployment script verifies the target before deploying:

| Check | Description |
|-------|-------------|
| SSH connectivity | Can reach the host via SSH |
| Docker installed | `docker` command exists |
| Docker daemon | `docker info` succeeds (daemon running, user has permissions) |
| Docker Compose | `docker compose version` works (v2 plugin required) |
| Disk space | Warns if less than 2 GB free (pi) or 1 GB free (speakers) |
| Images present | All required Docker images have been loaded |
| Network (speakers) | Speaker can reach RabbitMQ on the main host |

Every script asks for explicit user confirmation before deploying to any target machine.

## Environment Variables

### Build-time

| Variable | Default | Description |
|----------|---------|-------------|
| `PI_HOST` | `pi` | Hostname of the main Raspberry Pi |
| `SPEAKERS` | `speaker1 speaker3` | Space-separated list of speaker hostnames |
| `REGISTRY` | `sarah` | Image name prefix |
| `TAG` | `latest` | Image tag |
| `PLATFORM` | `linux/amd64` | Docker buildx platform |

### Runtime (pi/.env)

| Variable | Description |
|----------|-------------|
| `KEYCLOAK_ADMIN_PASSWORD` | Keycloak admin console password |
| `RABBITMQ_PASSWORD` | RabbitMQ password |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `ZWAVE_SERIAL_PORT` | Z-Wave USB device path |
| `PI_HOST` | Hostname for CORS origins |

### Runtime (speaker/.env)

| Variable | Description |
|----------|-------------|
| `PI_HOST` | Hostname of the main Raspberry Pi |
| `RABBITMQ_PASSWORD` | RabbitMQ password (must match pi) |

## Service Ports

After deployment, these services are accessible:

| Service | Host | URL |
|---------|------|-----|
| Frontend | pi | `http://pi:8081` |
| Keycloak | pi | `http://pi:8080` |
| RabbitMQ Management | pi | `http://pi:15672` |
| DeviceService | pi | `http://pi:5001` |
| PersonsService | pi | `http://pi:5002` |
| GeofencesService | pi | `http://pi:5003` |
| RoomService | pi | `http://pi:5004` |
| MonitoringService | pi | `http://pi:5005` |
| RulesService | pi | `http://pi:5006` |
| DashboardService | pi | `http://pi:5007` |
| SpeechServer | speaker1 | `http://speaker1:5008` |
| SpeechServer | speaker3 | `http://speaker3:5008` |

## Updating

To deploy a new version:

```bash
cd deploy
./deploy-all.sh
```

The scripts will rebuild all images, transfer them, and restart containers. Data volumes (Keycloak, RabbitMQ, PostgreSQL) are preserved across deployments.

Existing `.env` files on the targets are **never overwritten** — only uploaded on first deploy.

## Troubleshooting

### Check service logs
```bash
ssh pi 'cd /opt/sarah && docker compose logs -f deviceservice'
ssh speaker1 'cd /opt/sarah && docker compose logs -f speechserver'
```

### Restart a single service
```bash
ssh pi 'cd /opt/sarah && docker compose restart rulesservice'
```

### Full restart
```bash
ssh pi 'cd /opt/sarah && docker compose down && docker compose up -d'
```

### Reset all data (destructive)
```bash
ssh pi 'cd /opt/sarah && docker compose down -v'
```
