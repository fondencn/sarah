#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────────────
# build-and-push.sh — Build all Docker images and transfer to targets
# ────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

REGISTRY="${REGISTRY:-sarah}"
TAG="${TAG:-latest}"
PLATFORM="${PLATFORM:-linux/amd64}"

PI_HOST="${PI_HOST:-pi}"
SPEAKERS="${SPEAKERS:-speaker1 speaker3}"
SSH_USER="${SSH_USER:-pi}"

host_target() {
  local host="$1"
  echo "${SSH_USER}@${host}"
}

# All microservice images (context = repo root)
PI_IMAGES=(
  "deviceservice   Microservices/Sarah.DeviceService.WebApi/Dockerfile"
  "personsservice  Microservices/Sarah.Persons.WebApi/Dockerfile"
  "geofencesservice Microservices/Sarah.Geofences.WebApi/Dockerfile"
  "roomservice     Microservices/Sarah.RoomService.WebApi/Dockerfile"
  "monitoringservice Microservices/Sarah.Monitoring.WebApi/Dockerfile"
  "rulesservice    Microservices/Sarah.Rules.WebApi/Dockerfile"
  "dashboardservice Microservices/Sarah.Dashboard.WebApi/Dockerfile"
)

SPEAKER_IMAGES=(
  "speechserver    Microservices/Sarah.SpeechServer.WebApi/Dockerfile"
)

FRONTEND_IMAGE="frontend sarah.client/Dockerfile"

# ── Helpers ──────────────────────────────────────────────────────────

log()  { echo -e "\033[1;34m>>>\033[0m $*"; }
err()  { echo -e "\033[1;31m!!!\033[0m $*" >&2; }
ok()   { echo -e "\033[1;32m✓\033[0m $*"; }

confirm() {
  local msg="${1:-Continue?}"
  read -rp "$msg [y/N] " answer
  [[ "$answer" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }
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
  local name="$1" dockerfile="$2"
  local full_tag="${REGISTRY}/${name}:${TAG}"
  log "Building ${full_tag} (platform: ${PLATFORM})"

  if docker_has_buildx; then
    docker buildx build \
      --platform "${PLATFORM}" \
      --file "${REPO_ROOT}/${dockerfile}" \
      --tag "${full_tag}" \
      --load \
      "${REPO_ROOT}"
  elif docker_build_supports_platform; then
    log "docker buildx not found; using docker build instead"
    docker build \
      --platform "${PLATFORM}" \
      --file "${REPO_ROOT}/${dockerfile}" \
      --tag "${full_tag}" \
      "${REPO_ROOT}"
  else
    local host_platform
    host_platform="$(current_platform)"

    if [[ "${PLATFORM}" != "${host_platform}" ]]; then
      err "docker buildx is unavailable and docker build does not support --platform."
      err "Requested ${PLATFORM}, but this machine builds ${host_platform}."
      err "Install the buildx plugin or build on a ${PLATFORM} host."
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

# ── Main ─────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sarah Smart Home — Build & Push"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  Registry:  ${REGISTRY}"
echo "  Tag:       ${TAG}"
echo "  Platform:  ${PLATFORM}"
echo "  Pi host:   ${PI_HOST}"
echo "  Speakers:  ${SPEAKERS}"
echo "  SSH user:  ${SSH_USER}"
echo ""

confirm "Build all images and transfer to target hosts?"

# Build pi images
for entry in "${PI_IMAGES[@]}"; do
  read -r name dockerfile <<< "$entry"
  build_image "$name" "$dockerfile"
done

# Build speechserver
for entry in "${SPEAKER_IMAGES[@]}"; do
  read -r name dockerfile <<< "$entry"
  build_image "$name" "$dockerfile"
done

# Build frontend
read -r name dockerfile <<< "$FRONTEND_IMAGE"
build_image "$name" "$dockerfile"

echo ""
log "All images built successfully."
confirm "Transfer images to target hosts via SSH?"

# Transfer pi images + frontend
for entry in "${PI_IMAGES[@]}"; do
  read -r name _ <<< "$entry"
  transfer_image "$name" "$PI_HOST"
done
read -r name _ <<< "$FRONTEND_IMAGE"
transfer_image "$name" "$PI_HOST"

# Transfer speechserver to speakers
for entry in "${SPEAKER_IMAGES[@]}"; do
  read -r name _ <<< "$entry"
  for speaker in $SPEAKERS; do
    transfer_image "$name" "$speaker"
  done
done

echo ""
ok "All images transferred."
