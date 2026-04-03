# Deployment Guide

This guide covers building, transferring, and deploying the Sarah Smart Home system to the target Raspberry Pi infrastructure described in [infrastructure.md](infrastructure.md).

## Overview

The deployment uses Docker containers orchestrated with Docker Compose. Images are built on the development machine and transferred to the targets via SSH (`docker save` / `docker load`).

```
Build machine ── docker build/buildx ──► docker save ── ssh ──► docker load ── docker compose up
```

## Current Deployed State

The deployment has been validated on the current Raspberry Pi target set with this layout:

- `pi`: Raspberry Pi OS / Debian Bookworm, `arm64`, runs the main stack
- `speaker1`: Raspberry Pi OS Bullseye, `armv7`, runs `SpeechServer` with microphone recognition enabled
- `speaker3`: Raspberry Pi OS Bullseye, `armv7`, runs `SpeechServer` with text-to-speech enabled and voice recognition disabled because the ReSpeaker hat is currently broken

Operational status at the time of writing:

- Main stack on `pi` is deployed and reachable
- `speaker1` is healthy on `http://speaker1:5008`
- `speaker3` is healthy on `http://speaker3:5008`
- Speaker containers require `deploy/speaker/asound.conf` to be mounted so ALSA can resolve numeric device indices inside Docker
- Speaker containers require both `/dev/gpiomem` and `/dev/spidev0.0` for ReSpeaker LED ring GPIO/SPI access from inside Docker

Current speaker-specific behavior:

- `speaker1`: `SPEECH_RECOGNITION_ENABLED=true`
- `speaker3`: `SPEECH_RECOGNITION_ENABLED=false`

This is a temporary operational workaround until the microphone hardware on `speaker3` is replaced or repaired.

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
    ├── asound.conf            # ALSA config for containerized speaker access
    ├── docker-compose.yml     # Speaker satellite compose definition
    └── .env.example           # Environment template
```

## Deployment Checklist

Work through this checklist before the first real deployment:

- [ ] Install Docker on the build machine and verify both `docker buildx version` and `docker compose version`
- [ ] Install Docker on `pi`, `speaker1`, and `speaker3`
- [ ] Add your remote deployment user to the `docker` group on each target so `docker info` works without `sudo`
- [ ] Ensure hostnames or IPs for `pi`, `speaker1`, and `speaker3` are reachable from the build machine
- [ ] Set up SSH key-based login from the build machine to all target hosts
- [ ] Verify passwordless SSH works with `ssh pi`, `ssh speaker1`, and `ssh speaker3`
- [ ] Create `deploy/pi/.env` from `deploy/pi/.env.example` and replace every `CHANGE_ME` value
- [ ] Create either one shared `deploy/speaker/.env` or one file per speaker such as `deploy/speaker/speaker1.env` and `deploy/speaker/speaker3.env`
- [ ] Set a unique `SPEAKER_LOCATION` for each speaker env file
- [ ] Confirm `RABBITMQ_PASSWORD` is identical on `pi` and every speaker env file
- [ ] Enable SPI and GPIO on each speaker host with `sudo raspi-config` → Interface Options → SPI / GPIO, then reboot
- [ ] Verify the audio device exists on each speaker: `aplay -l` should list a capture/playback device
- [ ] If a speaker microphone hat is missing or broken, set `SPEECH_RECOGNITION_ENABLED=false` in that speaker's env file before deploying
- [ ] Verify GPIO and SPI groups on each speaker host: `getent group audio gpio spi` — set `AUDIO_GID`, `GPIO_GID`, and `SPI_GID` in each speaker env file when host values differ from defaults
- [ ] Confirm `PI_HOST` resolves correctly from both the build machine and the speaker machines
- [ ] Run `cd deploy && ./deploy-all.sh`
- [ ] Open the frontend at `http://pi:8081` after deployment completes

## SSH Key Setup

The deployment scripts call `ssh` and `scp` directly. The cleanest setup is to use SSH host aliases in `~/.ssh/config` and authenticate with a key pair.

### 1. Generate a deployment key pair on the build machine

```bash
ssh-keygen -t ed25519 -f ~/.ssh/sarah-deploy -C "sarah deployment"
```

### 2. Copy the public key to each target host

```bash
ssh-copy-id -i ~/.ssh/sarah-deploy.pub <user>@pi
ssh-copy-id -i ~/.ssh/sarah-deploy.pub <user>@speaker1
ssh-copy-id -i ~/.ssh/sarah-deploy.pub <user>@speaker3
```

If `ssh-copy-id` is unavailable, append `~/.ssh/sarah-deploy.pub` to `~/.ssh/authorized_keys` on each target manually.

### 3. Create SSH aliases on the build machine

Add this to `~/.ssh/config`:

```sshconfig
Host pi
    HostName 192.168.1.10
    User your-user
    IdentityFile ~/.ssh/sarah-deploy

Host speaker1
    HostName 192.168.1.21
    User your-user
    IdentityFile ~/.ssh/sarah-deploy

Host speaker3
    HostName 192.168.1.23
    User your-user
    IdentityFile ~/.ssh/sarah-deploy
```

Adjust the IP addresses and user names to match your machines.

### 4. Verify key-based login

```bash
ssh pi 'hostname'
ssh speaker1 'hostname'
ssh speaker3 'hostname'
```

All three commands should succeed without prompting for a password.

## Quick Start

### 1. Configure Environment

```bash
# Main host
cp deploy/pi/.env.example deploy/pi/.env
# Edit deploy/pi/.env — set all CHANGE_ME passwords

# Speakers: shared config for all speaker hosts
cp deploy/speaker/.env.example deploy/speaker/.env
# Edit deploy/speaker/.env — set RabbitMQ password (must match pi)

# Optional: per-speaker overrides with unique SPEAKER_LOCATION values
cp deploy/speaker/.env.example deploy/speaker/speaker1.env
cp deploy/speaker/.env.example deploy/speaker/speaker3.env
```

If `deploy/speaker/speaker1.env` or `deploy/speaker/speaker3.env` exists, `deploy-speakers.sh` uses that file for the matching host. Otherwise it falls back to `deploy/speaker/.env`.

For the currently deployed setup, `speaker3.env` also contains:

```bash
SPEECH_RECOGNITION_ENABLED=false
```

This keeps `speaker3` operational for playback while skipping Azure microphone initialization.

### 2. Full Deployment (one command)

```bash
cd deploy
./deploy-all.sh
```

This will:
1. Build Docker images for the detected target architectures (`linux/arm64` for `pi`, `linux/arm/v7` for speakers by default)
2. Transfer images to `pi`, `speaker1`, and `speaker3` via SSH
3. Run pre-flight checks (Docker version, disk space, connectivity)
4. Ask for confirmation before each deployment step
5. Deploy the main stack to `pi`
6. Deploy SpeechServer to `speaker1` and `speaker3`

If you need to refresh an existing remote speaker `.env`, run:

```bash
cd deploy
FORCE_ENV_UPLOAD=true ./deploy-speakers.sh
```

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
| Audio controls (speakers) | If `SPEAKER_PLAYBACK_VOLUME` is configured, prints `amixer scontrols` and warns when configured `SPEAKER_PLAYBACK_CONTROL` is not available |

Every script asks for explicit user confirmation before deploying to any target machine.

## Environment Variables

### Build-time

| Variable | Default | Description |
|----------|---------|-------------|
| `PI_HOST` | `pi` | Hostname of the main Raspberry Pi |
| `SPEAKERS` | `speaker1 speaker3` | Space-separated list of speaker hostnames |
| `REGISTRY` | `sarah` | Image name prefix |
| `TAG` | `latest` | Image tag |
| `PI_PLATFORM` | `linux/arm64` | Docker platform used for images deployed to `pi` |
| `SPEAKER_PLATFORM` | `linux/arm/v7` | Docker platform used for images deployed to speaker hosts |
| `SSH_USER` | `pi` | SSH username used by deployment scripts |
| `FORCE_ENV_UPLOAD` | `false` | When `true`, `deploy-speakers.sh` overwrites the remote speaker `.env` during deployment |

### Runtime (pi/.env)

| Variable | Description |
|----------|-------------|
| `KEYCLOAK_ADMIN_PASSWORD` | Keycloak admin console password |
| `RABBITMQ_PASSWORD` | RabbitMQ password |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `ZWAVE_SERIAL_PORT` | Z-Wave USB device path |
| `OPENWEATHER_API_KEY` | OpenWeatherMap API key used by MonitoringService |
| `PI_HOST` | Hostname for CORS origins |

### Runtime (speaker/.env)

| Variable | Description |
|----------|-------------|
| `PI_HOST` | Hostname of the main Raspberry Pi |
| `RABBITMQ_PASSWORD` | RabbitMQ password (must match pi) |
| `AZURE_SPEECH_KEY` | Azure Speech subscription key |
| `AZURE_SPEECH_REGION` | Azure Speech region |
| `SPEAKER_LOCATION` | Room name for this speaker |
| `SPEECH_RECOGNITION_ENABLED` | Optional per-speaker override. Set to `false` to disable microphone recognition while keeping playback enabled |
| `SPEAKER_PLAYBACK_CONTROL` | Optional ALSA control name to set after deployment. If unset, deploy script auto-detects one (`Headphone` → `Speaker` → `PCM` → `Master`) |
| `SPEAKER_PLAYBACK_VOLUME` | Optional playback volume applied inside container after startup (example: `35%`) |

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

Existing speaker `.env` files on the targets are only uploaded on first deploy by default. Use `FORCE_ENV_UPLOAD=true ./deploy-speakers.sh` when you intentionally want to refresh them.

## Current Limitations

- `deploy-speakers.sh` always syncs `docker-compose.yml` and `asound.conf`, but it only refreshes the remote speaker `.env` when `FORCE_ENV_UPLOAD=true`
- If an earlier failed deployment left behind stale containers, run `docker compose up -d --force-recreate --remove-orphans` on the target to cleanly reconcile the state

## Troubleshooting

### Apply updated speaker config manually

Use this when `asound.conf` or a per-speaker env file changed after the first deploy:

```bash
cd deploy
FORCE_ENV_UPLOAD=true ./deploy-speakers.sh
```

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
