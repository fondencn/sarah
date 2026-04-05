#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────────────
# deploy-pi.sh — Deploy the main stack to the Raspberry Pi "pi"
# ────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PI_HOST="${PI_HOST:-pi}"
DEPLOY_DIR="${REMOTE_DEPLOY_DIR:-/opt/sarah}"
SSH_USER="${SSH_USER:-pi}"
PI_TARGET="${SSH_USER}@${PI_HOST}"

# ── Helpers ──────────────────────────────────────────────────────────

log()  { echo -e "\033[1;34m>>>\033[0m $*"; }
err()  { echo -e "\033[1;31m!!!\033[0m $*" >&2; }
ok()   { echo -e "\033[1;32m✓\033[0m $*"; }

confirm() {
  local msg="${1:-Continue?}"
  read -rp "$msg [y/N] " answer
  [[ "$answer" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }
}

remote() {
  ssh "${PI_TARGET}" "$@"
}

# Append any variables present in the local .env that are absent from the
# remote .env.  This is a one-way, additive patch — existing remote values
# are never touched.  Used to propagate newly added vars (e.g. Grafana creds)
# to an already-initialised deployment without a full env refresh.
patch_missing_env_vars() {
  local local_env="${SCRIPT_DIR}/pi/.env"
  [[ -f "$local_env" ]] || return 0

  log "Patching remote .env with any missing variables..."
  local patched=0

  while IFS= read -r line; do
    [[ -z "$line" || "$line" == \#* ]] && continue
    local key="${line%%=*}"
    [[ -z "$key" ]] && continue
    if ! remote "grep -qF '${key}=' '${DEPLOY_DIR}/.env'" 2>/dev/null; then
      remote "cat >> '${DEPLOY_DIR}/.env'" <<< "$line"
      log "  Patched missing var: ${key}"
      patched=1
    fi
  done < "$local_env"

  if (( patched )); then
    ok "Remote .env patched with new variables."
  else
    ok "Remote .env is up-to-date — no new variables needed."
  fi
}

# ── Pre-flight checks ───────────────────────────────────────────────

preflight_check() {
  log "Running pre-flight checks on ${PI_HOST}..."

  # 1. SSH connectivity
  log "  Checking SSH connectivity..."
  if ! ssh -o ConnectTimeout=5 -o BatchMode=yes "${PI_TARGET}" 'echo ok' &>/dev/null; then
    err "Cannot connect to ${PI_TARGET} via SSH."
    err "Ensure SSH is configured (key-based auth recommended) and the host is reachable."
    exit 1
  fi
  ok "  SSH connection to ${PI_TARGET}"

  # 2. Docker installed
  log "  Checking Docker installation..."
  if ! remote 'command -v docker' &>/dev/null; then
    err "Docker is not installed on ${PI_HOST}."
    err "Install Docker: curl -fsSL https://get.docker.com | sh"
    exit 1
  fi
  ok "  Docker is installed"

  # 3. Docker daemon running
  log "  Checking Docker daemon..."
  if ! remote 'docker info' &>/dev/null; then
    err "Docker daemon is not running or current user lacks permissions on ${PI_HOST}."
    err "Ensure the docker service is running and the user is in the 'docker' group."
    exit 1
  fi
  ok "  Docker daemon is running"

  # 4. Docker Compose available
  log "  Checking Docker Compose..."
  if ! remote 'docker compose version' &>/dev/null; then
    err "Docker Compose (v2 plugin) is not available on ${PI_HOST}."
    err "Install it: sudo apt-get install docker-compose-plugin"
    exit 1
  fi
  ok "  Docker Compose is available"

  # 5. Disk space (warn if <2GB free on /)
  log "  Checking disk space..."
  local free_kb
  free_kb=$(remote 'df --output=avail / | tail -1' 2>/dev/null | tr -d ' ')
  if [[ -n "$free_kb" ]] && (( free_kb < 2097152 )); then
    err "  WARNING: Less than 2 GB free disk space on ${PI_HOST} (${free_kb} KB available)."
    confirm "  Continue anyway?"
  else
    ok "  Disk space OK ($(( free_kb / 1024 )) MB free)"
  fi

  # 6. Required images present
  log "  Checking Docker images..."
  local missing=0
  for img in deviceservice personsservice geofencesservice roomservice monitoringservice rulesservice dashboardservice frontend; do
    if ! remote "docker image inspect sarah/${img}:latest" &>/dev/null; then
      err "  Image sarah/${img}:latest not found on ${PI_HOST}. Run build-and-push.sh first."
      missing=1
    fi
  done
  if (( missing )); then
    exit 1
  fi
  ok "  All required images present"

  echo ""
  ok "All pre-flight checks passed for ${PI_HOST}."
}

# ── Deploy ───────────────────────────────────────────────────────────

deploy() {
  log "Uploading compose files to ${PI_TARGET}:${DEPLOY_DIR}/"
  remote "mkdir -p ${DEPLOY_DIR}"
  scp "${SCRIPT_DIR}/pi/docker-compose.yml" "${PI_TARGET}:${DEPLOY_DIR}/docker-compose.yml"
  scp "${SCRIPT_DIR}/pi/init-databases.sh" "${PI_TARGET}:${DEPLOY_DIR}/init-databases.sh"

  log "Uploading observability configs to ${PI_TARGET}:${DEPLOY_DIR}/otel/"
  scp -r "${SCRIPT_DIR}/pi/otel" "${PI_TARGET}:${DEPLOY_DIR}/"
  ok "Uploaded OTel configs."

  # Upload Keycloak realm import file (always update — Keycloak skips import if realm already exists)
  local realm_file="${SCRIPT_DIR}/../Sarah.AppHost/sarah-realm-realm.json"
  if [[ -f "$realm_file" ]]; then
    scp "$realm_file" "${PI_TARGET}:${DEPLOY_DIR}/sarah-realm-realm.json"
    ok "Uploaded Keycloak realm import file."
  else
    err "Keycloak realm file not found at ${realm_file}"
    err "The sarah-realm will not be auto-imported on first start."
    confirm "Continue without realm import?"
  fi

  # Upload .env only if it doesn't exist on target (don't overwrite secrets)
  if ! remote "test -f ${DEPLOY_DIR}/.env"; then
    if [[ -f "${SCRIPT_DIR}/pi/.env" ]]; then
      scp "${SCRIPT_DIR}/pi/.env" "${PI_TARGET}:${DEPLOY_DIR}/.env"
      log "Uploaded .env file. Review and edit passwords on the target."
    else
      err "No .env file found at ${SCRIPT_DIR}/pi/.env"
      err "Copy .env.example to .env and configure it before deploying."
      exit 1
    fi
  else
    ok ".env already exists on target — not overwriting."
  fi

  patch_missing_env_vars

  # Make init script executable
  remote "chmod +x ${DEPLOY_DIR}/init-databases.sh"

  # Warn if volumes already exist (data will be preserved)
  local existing_volumes
  existing_volumes=$(remote "docker volume ls --filter name=sarah -q 2>/dev/null" || true)
  if [[ -n "$existing_volumes" ]]; then
    ok "Existing data volumes detected — databases will be preserved."
    log "  Volumes: $(echo "$existing_volumes" | tr '\n' ' ')"
  fi

  log "Starting services on ${PI_HOST}..."
  # Start infrastructure first to avoid startup races (DB/RabbitMQ can be briefly unavailable during init).
  remote "cd ${DEPLOY_DIR} && docker compose up -d postgres rabbitmq keycloak"

  log "Waiting for PostgreSQL and RabbitMQ health checks..."
  remote "bash -lc 'cd \"${DEPLOY_DIR}\" && for i in \$(seq 1 60); do pg_id=\$(docker compose ps -q postgres); mq_id=\$(docker compose ps -q rabbitmq); pg=\$(docker inspect --format=\"{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}\" \"\$pg_id\" 2>/dev/null || echo unknown); mq=\$(docker inspect --format=\"{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}\" \"\$mq_id\" 2>/dev/null || echo unknown); if [ \"\$pg\" = \"healthy\" ] && [ \"\$mq\" = \"healthy\" ]; then exit 0; fi; sleep 2; done; echo \"Timed out waiting for infrastructure health checks\" >&2; docker compose ps >&2; exit 1'"

  # Start app services after infrastructure is confirmed healthy.
  remote "cd ${DEPLOY_DIR} && docker compose up -d deviceservice personsservice geofencesservice roomservice monitoringservice rulesservice dashboardservice frontend"

  log "Starting observability stack on ${PI_HOST}..."
  remote "cd ${DEPLOY_DIR} && docker compose --profile observability up -d otel-collector victoriametrics loki tempo grafana"
  ok "Observability stack started. Grafana: http://${PI_HOST}:3000"

  echo ""
  ok "Deployment to ${PI_HOST} complete. All data volumes preserved."
  log "View logs: ssh ${PI_TARGET} 'cd ${DEPLOY_DIR} && docker compose logs -f'"
}

# ── Main ─────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sarah Smart Home — Deploy to main host (${PI_HOST})"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  SSH target: ${PI_TARGET}"
echo ""

preflight_check

echo ""
echo "This will deploy the following to ${PI_HOST}:"
echo "  • Keycloak (identity provider)"
echo "  • RabbitMQ (message broker)"
echo "  • PostgreSQL (databases: devices, persons, monitoring, rules, rooms, dashboard)"
echo "  • DeviceService, PersonsService, GeofencesService, RoomService"
echo "  • MonitoringService, RulesService, DashboardService"
echo "  • Angular frontend (nginx)"
echo "  • OTel stack: otel-collector, VictoriaMetrics, Loki, Tempo, Grafana"
echo ""

confirm "Deploy to ${PI_HOST}?"

deploy
