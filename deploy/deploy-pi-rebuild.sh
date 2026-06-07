#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────────────
# deploy-pi-rebuild.sh — Build selected pi images and deploy them to pi
# ────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

REGISTRY="${REGISTRY:-sarah}"
TAG="${TAG:-latest}"
PI_PLATFORM="${PI_PLATFORM:-linux/arm64}"
PI_HOST="${PI_HOST:-pi}"
DEPLOY_DIR="${REMOTE_DEPLOY_DIR:-/opt/sarah}"
SSH_USER="${SSH_USER:-pi}"
PI_TARGET="${SSH_USER}@${PI_HOST}"

declare -A PI_SERVICE_DOCKERFILES=(
  [deviceservice]="Microservices/Sarah.DeviceService.WebApi/Dockerfile"
  [personsservice]="Microservices/Sarah.Persons.WebApi/Dockerfile"
  [geofencesservice]="Microservices/Sarah.Geofences.WebApi/Dockerfile"
  [roomservice]="Microservices/Sarah.RoomService.WebApi/Dockerfile"
  [monitoringservice]="Microservices/Sarah.Monitoring.WebApi/Dockerfile"
  [rulesservice]="Microservices/Sarah.Rules.WebApi/Dockerfile"
  [dashboardservice]="Microservices/Sarah.Dashboard.WebApi/Dockerfile"
  [adminservice]="Microservices/Sarah.Admin.WebApi/Dockerfile"
  [frontend]="sarah.client/Dockerfile"
)

ALL_PI_SERVICES=(
  deviceservice
  personsservice
  geofencesservice
  roomservice
  monitoringservice
  rulesservice
  dashboardservice
  adminservice
  frontend
)

SELECTED_SERVICES=()

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

host_target() {
  local host="$1"
  echo "${SSH_USER}@${host}"
}

current_platform() {
  case "$(uname -m)" in
    x86_64|amd64) echo "linux/amd64" ;;
    aarch64|arm64) echo "linux/arm64" ;;
    armv7l) echo "linux/arm/v7" ;;
    *) echo "unknown" ;;
  esac
}

docker_has_buildx() {
  docker buildx version >/dev/null 2>&1
}

docker_build_supports_platform() {
  docker build --help 2>/dev/null | grep -q -- '--platform'
}

build_image() {
  local name="$1" dockerfile="$2" platform="$3"
  local full_tag="${REGISTRY}/${name}:${TAG}"
  log "Building ${full_tag} (platform: ${platform})"

  if docker_has_buildx; then
    docker buildx build \
      --platform "${platform}" \
      --file "${REPO_ROOT}/${dockerfile}" \
      --tag "${full_tag}" \
      --load \
      "${REPO_ROOT}"
  elif docker_build_supports_platform; then
    log "docker buildx not found; using docker build instead"
    docker build \
      --platform "${platform}" \
      --file "${REPO_ROOT}/${dockerfile}" \
      --tag "${full_tag}" \
      "${REPO_ROOT}"
  else
    local host_platform
    host_platform="$(current_platform)"

    if [[ "${platform}" != "${host_platform}" ]]; then
      err "docker buildx is unavailable and docker build does not support --platform."
      err "Requested ${platform}, but this machine builds ${host_platform}."
      err "Install the buildx plugin or build on a ${platform} host."
      exit 1
    fi

    log "docker buildx not found; using docker build for host platform ${host_platform}"
    docker build \
      --file "${REPO_ROOT}/${dockerfile}" \
      --tag "${full_tag}" \
      "${REPO_ROOT}"
  fi

  ok "Built ${full_tag}"
}

transfer_image() {
  local name="$1" host="$2"
  local target
  target="$(host_target "$host")"
  local full_tag="${REGISTRY}/${name}:${TAG}"
  log "Transferring ${full_tag} → ${target}"
  docker save "${full_tag}" | ssh "${target}" 'docker load'
  ok "Transferred ${full_tag} → ${target}"
}

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

preflight_check() {
  log "Running pre-flight checks on ${PI_HOST}..."

  log "  Checking SSH connectivity..."
  if ! ssh -o ConnectTimeout=5 -o BatchMode=yes "${PI_TARGET}" 'echo ok' &>/dev/null; then
    err "Cannot connect to ${PI_TARGET} via SSH."
    err "Ensure SSH is configured (key-based auth recommended) and the host is reachable."
    exit 1
  fi
  ok "  SSH connection to ${PI_TARGET}"

  log "  Checking Docker installation..."
  if ! remote 'command -v docker' &>/dev/null; then
    err "Docker is not installed on ${PI_HOST}."
    err "Install Docker: curl -fsSL https://get.docker.com | sh"
    exit 1
  fi
  ok "  Docker is installed"

  log "  Checking Docker daemon..."
  if ! remote 'docker info' &>/dev/null; then
    err "Docker daemon is not running or current user lacks permissions on ${PI_HOST}."
    err "Ensure the docker service is running and the user is in the 'docker' group."
    exit 1
  fi
  ok "  Docker daemon is running"

  log "  Checking Docker Compose..."
  if ! remote 'docker compose version' &>/dev/null; then
    err "Docker Compose (v2 plugin) is not available on ${PI_HOST}."
    err "Install it: sudo apt-get install docker-compose-plugin"
    exit 1
  fi
  ok "  Docker Compose is available"

  log "  Checking disk space..."
  local free_kb
  free_kb=$(remote 'df --output=avail / | tail -1' 2>/dev/null | tr -d ' ')
  if [[ -n "$free_kb" ]] && (( free_kb < 2097152 )); then
    err "  WARNING: Less than 2 GB free disk space on ${PI_HOST} (${free_kb} KB available)."
    confirm "  Continue anyway?"
  else
    ok "  Disk space OK ($(( free_kb / 1024 )) MB free)"
  fi

  log "  Checking Docker images..."
  local missing=0
  for service in "${SELECTED_SERVICES[@]}"; do
    if ! remote "docker image inspect ${REGISTRY}/${service}:${TAG}" &>/dev/null; then
      err "  Image ${REGISTRY}/${service}:${TAG} not found on ${PI_HOST}."
      missing=1
    fi
  done
  if (( missing )); then
    exit 1
  fi
  ok "  Selected images present"

  echo ""
  ok "All pre-flight checks passed for ${PI_HOST}."
}

deploy() {
  log "Uploading compose files to ${PI_TARGET}:${DEPLOY_DIR}/"
  remote "mkdir -p ${DEPLOY_DIR}"
  scp "${SCRIPT_DIR}/pi/docker-compose.yml" "${PI_TARGET}:${DEPLOY_DIR}/docker-compose.yml"
  scp "${SCRIPT_DIR}/pi/init-databases.sh" "${PI_TARGET}:${DEPLOY_DIR}/init-databases.sh"

  log "Uploading observability configs to ${PI_TARGET}:${DEPLOY_DIR}/otel/"
  scp -r "${SCRIPT_DIR}/pi/otel" "${PI_TARGET}:${DEPLOY_DIR}/"
  ok "Uploaded OTel configs."

  local realm_file="${SCRIPT_DIR}/../Sarah.AppHost/sarah-realm-realm.json"
  if [[ -f "$realm_file" ]]; then
    scp "$realm_file" "${PI_TARGET}:${DEPLOY_DIR}/sarah-realm-realm.json"
    ok "Uploaded Keycloak realm import file."
  else
    err "Keycloak realm file not found at ${realm_file}"
    err "The sarah-realm will not be auto-imported on first start."
    confirm "Continue without realm import?"
  fi

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

  remote "chmod +x ${DEPLOY_DIR}/init-databases.sh"

  local existing_volumes
  existing_volumes=$(remote "docker volume ls --filter name=sarah -q 2>/dev/null" || true)
  if [[ -n "$existing_volumes" ]]; then
    ok "Existing data volumes detected — databases will be preserved."
    log "  Volumes: $(echo "$existing_volumes" | tr '\n' ' ')"
  fi

  log "Starting infrastructure on ${PI_HOST}..."
  remote "cd ${DEPLOY_DIR} && docker compose up -d postgres rabbitmq keycloak"

  log "Waiting for PostgreSQL and RabbitMQ health checks..."
  remote "bash -lc 'cd "${DEPLOY_DIR}" && for i in \$(seq 1 60); do pg_id=\$(docker compose ps -q postgres); mq_id=\$(docker compose ps -q rabbitmq); pg=\$(docker inspect --format="{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}" "\$pg_id" 2>/dev/null || echo unknown); mq=\$(docker inspect --format="{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}" "\$mq_id" 2>/dev/null || echo unknown); if [ "\$pg" = "healthy" ] && [ "\$mq" = "healthy" ]; then exit 0; fi; sleep 2; done; echo "Timed out waiting for infrastructure health checks" >&2; docker compose ps >&2; exit 1'"

  log "Starting selected app services on ${PI_HOST}..."
  remote "cd ${DEPLOY_DIR} && docker compose up -d ${SELECTED_SERVICES[*]}"

  log "Starting observability stack on ${PI_HOST}..."
  remote "cd ${DEPLOY_DIR} && docker compose --profile observability up -d otel-collector victoriametrics loki tempo grafana"
  ok "Observability stack started. Grafana: http://${PI_HOST}:3000"

  echo ""
  ok "Deployment to ${PI_HOST} complete. All data volumes preserved."
  log "View logs: ssh ${PI_TARGET} 'cd ${DEPLOY_DIR} && docker compose logs -f'"
}

usage() {
  cat <<'EOF'
Usage: deploy-pi-rebuild.sh [service ...]

Builds, transfers, and deploys selected pi services.
If no services are provided, all pi services and the frontend are rebuilt and deployed.

Recognized services:
  deviceservice personsservice geofencesservice roomservice
  monitoringservice rulesservice dashboardservice adminservice frontend
EOF
}

parse_services() {
  if (($# == 0)); then
    SELECTED_SERVICES=("${ALL_PI_SERVICES[@]}")
    return 0
  fi

  if (($# == 1)) && [[ "$1" == "all" ]]; then
    SELECTED_SERVICES=("${ALL_PI_SERVICES[@]}")
    return 0
  fi

  declare -A seen=()
  for service in "$@"; do
    if [[ -z "${PI_SERVICE_DOCKERFILES[$service]+x}" ]]; then
      err "Unknown service '${service}'."
      err "Run '${0##*/} --help' to see the valid service names."
      exit 1
    fi
    if [[ -z "${seen[$service]+x}" ]]; then
      SELECTED_SERVICES+=("$service")
      seen[$service]=1
    fi
  done
}

# ── Main ─────────────────────────────────────────────────────────────

if [[ ${1:-} == "-h" || ${1:-} == "--help" ]]; then
  usage
  exit 0
fi

parse_services "$@"

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sarah Smart Home — Build, Transfer & Deploy to ${PI_HOST}"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  Registry:  ${REGISTRY}"
echo "  Tag:       ${TAG}"
echo "  Pi platform:       ${PI_PLATFORM}"
echo "  Pi host:   ${PI_HOST}"
echo "  SSH user:  ${SSH_USER}"
echo ""
if [[ ${#SELECTED_SERVICES[@]} -eq ${#ALL_PI_SERVICES[@]} ]]; then
  echo "  Services:  all"
else
  echo "  Services:  ${SELECTED_SERVICES[*]}"
fi
echo ""

confirm "Build selected images and transfer them to ${PI_HOST}?"

for service in "${SELECTED_SERVICES[@]}"; do
  build_image "$service" "${PI_SERVICE_DOCKERFILES[$service]}" "$PI_PLATFORM"
done

echo ""
log "Selected images built successfully."
confirm "Transfer images to ${PI_HOST} via SSH?"

for service in "${SELECTED_SERVICES[@]}"; do
  transfer_image "$service" "$PI_HOST"
done

echo ""
preflight_check

confirm "Deploy selected services to ${PI_HOST}?"

deploy
