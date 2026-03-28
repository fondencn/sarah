#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────────────
# deploy-speakers.sh — Deploy SpeechServer to speaker satellites
# ────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PI_HOST="${PI_HOST:-pi}"
SPEAKERS="${SPEAKERS:-speaker1 speaker3}"
DEPLOY_DIR="${REMOTE_DEPLOY_DIR:-/opt/sarah}"

# ── Helpers ──────────────────────────────────────────────────────────

log()  { echo -e "\033[1;34m>>>\033[0m $*"; }
err()  { echo -e "\033[1;31m!!!\033[0m $*" >&2; }
ok()   { echo -e "\033[1;32m✓\033[0m $*"; }

confirm() {
  local msg="${1:-Continue?}"
  read -rp "$msg [y/N] " answer
  [[ "$answer" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }
}

# ── Pre-flight check for a single speaker ───────────────────────────

preflight_check_host() {
  local host="$1"
  log "Running pre-flight checks on ${host}..."

  # 1. SSH connectivity
  log "  Checking SSH connectivity..."
  if ! ssh -o ConnectTimeout=5 -o BatchMode=yes "${host}" 'echo ok' &>/dev/null; then
    err "Cannot connect to ${host} via SSH."
    err "Ensure SSH is configured (key-based auth recommended) and the host is reachable."
    return 1
  fi
  ok "  SSH connection to ${host}"

  # 2. Docker installed
  log "  Checking Docker installation..."
  if ! ssh "${host}" 'command -v docker' &>/dev/null; then
    err "Docker is not installed on ${host}."
    err "Install Docker: curl -fsSL https://get.docker.com | sh"
    return 1
  fi
  ok "  Docker is installed"

  # 3. Docker daemon running
  log "  Checking Docker daemon..."
  if ! ssh "${host}" 'docker info' &>/dev/null; then
    err "Docker daemon is not running or current user lacks permissions on ${host}."
    err "Ensure the docker service is running and the user is in the 'docker' group."
    return 1
  fi
  ok "  Docker daemon is running"

  # 4. Docker Compose available
  log "  Checking Docker Compose..."
  if ! ssh "${host}" 'docker compose version' &>/dev/null; then
    err "Docker Compose (v2 plugin) is not available on ${host}."
    err "Install it: sudo apt-get install docker-compose-plugin"
    return 1
  fi
  ok "  Docker Compose is available"

  # 5. Disk space
  log "  Checking disk space..."
  local free_kb
  free_kb=$(ssh "${host}" 'df --output=avail / | tail -1' 2>/dev/null | tr -d ' ')
  if [[ -n "$free_kb" ]] && (( free_kb < 1048576 )); then
    err "  WARNING: Less than 1 GB free disk space on ${host} (${free_kb} KB available)."
    confirm "  Continue anyway?"
  else
    ok "  Disk space OK ($(( free_kb / 1024 )) MB free)"
  fi

  # 6. SpeechServer image present
  log "  Checking Docker images..."
  if ! ssh "${host}" 'docker image inspect sarah/speechserver:latest' &>/dev/null; then
    err "  Image sarah/speechserver:latest not found on ${host}. Run build-and-push.sh first."
    return 1
  fi
  ok "  SpeechServer image present"

  # 7. Main host reachable from speaker
  log "  Checking connectivity to main host (${PI_HOST})..."
  if ! ssh "${host}" "timeout 3 bash -c '</dev/tcp/${PI_HOST}/5672'" &>/dev/null; then
    err "  WARNING: Cannot reach ${PI_HOST}:5672 (RabbitMQ) from ${host}."
    err "  Ensure the main host is deployed and RabbitMQ is running."
    confirm "  Continue anyway?"
  else
    ok "  Main host ${PI_HOST} reachable from ${host}"
  fi

  ok "All pre-flight checks passed for ${host}."
  return 0
}

# ── Deploy to a single speaker ───────────────────────────────────────

deploy_speaker() {
  local host="$1"
  local env_source="${SCRIPT_DIR}/speaker/${host}.env"

  log "Uploading compose files to ${host}:${DEPLOY_DIR}/"
  ssh "${host}" "mkdir -p ${DEPLOY_DIR}"
  scp "${SCRIPT_DIR}/speaker/docker-compose.yml" "${host}:${DEPLOY_DIR}/docker-compose.yml"

  # Upload .env only if it doesn't exist on target
  if ! ssh "${host}" "test -f ${DEPLOY_DIR}/.env"; then
    if [[ -f "$env_source" ]]; then
      scp "$env_source" "${host}:${DEPLOY_DIR}/.env"
      log "Uploaded ${host}.env to ${host}."
    elif [[ -f "${SCRIPT_DIR}/speaker/.env" ]]; then
      scp "${SCRIPT_DIR}/speaker/.env" "${host}:${DEPLOY_DIR}/.env"
      log "Uploaded shared speaker .env file to ${host}."
    else
      err "No speaker environment file found for ${host}."
      err "Create ${SCRIPT_DIR}/speaker/${host}.env or ${SCRIPT_DIR}/speaker/.env before deploying."
      exit 1
    fi
  else
    ok ".env already exists on ${host} — not overwriting."
  fi

  log "Starting SpeechServer on ${host}..."
  ssh "${host}" "cd ${DEPLOY_DIR} && docker compose up -d"

  ok "Deployment to ${host} complete."
}

# ── Main ─────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sarah Smart Home — Deploy SpeechServer to speakers"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  Main host:  ${PI_HOST}"
echo "  Speakers:   ${SPEAKERS}"
echo ""

# Run pre-flight on all speakers
all_ok=true
for speaker in $SPEAKERS; do
  echo ""
  if ! preflight_check_host "$speaker"; then
    err "Pre-flight failed for ${speaker}."
    all_ok=false
  fi
done

if [[ "$all_ok" != "true" ]]; then
  err "Some pre-flight checks failed. Fix the issues above before deploying."
  exit 1
fi

echo ""
echo "This will deploy the SpeechServer to: ${SPEAKERS}"
echo "  • Connects to RabbitMQ on ${PI_HOST}:5672"
echo "  • Connects to Keycloak on ${PI_HOST}:8080"
echo "  • Connects to DeviceService on ${PI_HOST}:5001"
echo ""

confirm "Deploy SpeechServer to all speakers?"

for speaker in $SPEAKERS; do
  echo ""
  deploy_speaker "$speaker"
done

echo ""
ok "All speakers deployed."
