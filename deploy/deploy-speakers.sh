#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────────────
# deploy-speakers.sh — Deploy SpeechServer to speaker satellites
# ────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PI_HOST="${PI_HOST:-pi}"
SPEAKERS="${SPEAKERS:-speaker1 speaker3}"
DEPLOY_DIR="${REMOTE_DEPLOY_DIR:-/opt/sarah}"
SSH_USER="${SSH_USER:-pi}"
FORCE_ENV_UPLOAD="${FORCE_ENV_UPLOAD:-false}"

host_target() {
  local host="$1"
  echo "${SSH_USER}@${host}"
}

# ── Helpers ──────────────────────────────────────────────────────────

log()  { echo -e "\033[1;34m>>>\033[0m $*"; }
err()  { echo -e "\033[1;31m!!!\033[0m $*" >&2; }
ok()   { echo -e "\033[1;32m✓\033[0m $*"; }

is_true() {
  [[ "${1,,}" == "true" || "${1}" == "1" || "${1,,}" == "yes" || "${1,,}" == "y" ]]
}

confirm() {
  local msg="${1:-Continue?}"
  read -rp "$msg [y/N] " answer
  [[ "$answer" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }
}

resolve_speaker_env() {
  local host="$1"
  local env_source="${SCRIPT_DIR}/speaker/${host}.env"

  if [[ -f "$env_source" ]]; then
    echo "$env_source"
  elif [[ -f "${SCRIPT_DIR}/speaker/.env" ]]; then
    echo "${SCRIPT_DIR}/speaker/.env"
  else
    return 1
  fi
}

get_env_value() {
  local file="$1"
  local key="$2"
  grep -E "^${key}=" "$file" | tail -n1 | cut -d'=' -f2-
}

preflight_check_audio_controls() {
  local host="$1"
  local target="$2"
  local env_source
  local playback_volume=""
  local requested_control=""
  local controls

  if env_source="$(resolve_speaker_env "$host" 2>/dev/null)"; then
    playback_volume="$(get_env_value "$env_source" "SPEAKER_PLAYBACK_VOLUME" || true)"
    requested_control="$(get_env_value "$env_source" "SPEAKER_PLAYBACK_CONTROL" || true)"
  fi

  if [[ -z "$playback_volume" ]]; then
    log "  No SPEAKER_PLAYBACK_VOLUME configured for ${host}; skipping mixer-control preflight."
    return 0
  fi

  log "  Reading ALSA mixer controls on ${host}..."
  controls="$(ssh "${target}" 'amixer scontrols 2>/dev/null' || true)"
  if [[ -z "$controls" ]]; then
    err "  WARNING: Could not read ALSA mixer controls on ${host}."
    err "  Volume application may fail; verify audio stack with: amixer scontrols"
    return 0
  fi

  while IFS= read -r control_line; do
    [[ -n "$control_line" ]] && log "    ${control_line}"
  done <<< "$controls"

  if [[ -n "$requested_control" ]]; then
    if echo "$controls" | grep -Fq "'$requested_control'"; then
      ok "  Configured playback control '${requested_control}' is available on ${host}."
    else
      err "  WARNING: Configured SPEAKER_PLAYBACK_CONTROL='${requested_control}' was not found on ${host}."
      err "  deploy-speakers.sh will auto-detect a valid control during deployment."
    fi
  fi
}

apply_playback_volume() {
  local target="$1"
  local volume="$2"
  local requested_control="${3:-}"

  if ! ssh "${target}" "bash -s" -- "${DEPLOY_DIR}" "${volume}" "${requested_control}" <<'EOF'; then
set -euo pipefail

deploy_dir="$1"
volume="$2"
requested_control="$3"

cd "$deploy_dir"
cid="$(docker compose ps -q speechserver)"
if [[ -z "$cid" ]]; then
  echo "SpeechServer container not running; cannot set playback volume."
  exit 1
fi

controls="$(docker exec "$cid" sh -lc 'amixer scontrols 2>/dev/null' || true)"
if [[ -z "$controls" ]]; then
  echo "No ALSA simple controls reported by amixer."
  exit 2
fi

candidates=()
if [[ -n "$requested_control" ]]; then
  candidates+=("$requested_control")
fi
candidates+=("Headphone" "Speaker" "PCM" "Master")

selected_control=""
for candidate in "${candidates[@]}"; do
  if echo "$controls" | grep -Fq "'$candidate'"; then
    selected_control="$candidate"
    break
  fi
done

if [[ -z "$selected_control" ]]; then
  selected_control="$(echo "$controls" | sed -n "s/.*'\([^']*\)'.*/\1/p" | head -n1)"
fi

if [[ -z "$selected_control" ]]; then
  echo "Could not determine a valid ALSA playback control from amixer output."
  exit 3
fi

echo "Using playback control: $selected_control"
docker exec "$cid" sh -lc "amixer sset \"$selected_control\" \"$volume\" && amixer sget \"$selected_control\" | sed -n '1,6p'"
EOF
    err "Playback volume setup failed on ${target}."
    err "Set SPEAKER_PLAYBACK_CONTROL explicitly in the speaker env if auto-detection picked the wrong control."
    return 1
  fi

  return 0
}

# ── Pre-flight check for a single speaker ───────────────────────────

preflight_check_host() {
  local host="$1"
  local target
  target="$(host_target "$host")"
  log "Running pre-flight checks on ${host}..."

  # 1. SSH connectivity
  log "  Checking SSH connectivity..."
  if ! ssh -o ConnectTimeout=5 -o BatchMode=yes "${target}" 'echo ok' &>/dev/null; then
    err "Cannot connect to ${target} via SSH."
    err "Ensure SSH is configured (key-based auth recommended) and the host is reachable."
    return 1
  fi
  ok "  SSH connection to ${target}"

  # 2. Docker installed
  log "  Checking Docker installation..."
  if ! ssh "${target}" 'command -v docker' &>/dev/null; then
    err "Docker is not installed on ${host}."
    err "Install Docker: curl -fsSL https://get.docker.com | sh"
    return 1
  fi
  ok "  Docker is installed"

  # 3. Docker daemon running
  log "  Checking Docker daemon..."
  if ! ssh "${target}" 'docker info' &>/dev/null; then
    err "Docker daemon is not running or current user lacks permissions on ${host}."
    err "Ensure the docker service is running and the user is in the 'docker' group."
    return 1
  fi
  ok "  Docker daemon is running"

  # 4. Docker Compose available
  log "  Checking Docker Compose..."
  if ! ssh "${target}" 'docker compose version' &>/dev/null; then
    err "Docker Compose (v2 plugin) is not available on ${host}."
    err "Install it: sudo apt-get install docker-compose-plugin"
    return 1
  fi
  ok "  Docker Compose is available"

  # 5. Disk space
  log "  Checking disk space..."
  local free_kb
  free_kb=$(ssh "${target}" 'df --output=avail / | tail -1' 2>/dev/null | tr -d ' ')
  if [[ -n "$free_kb" ]] && (( free_kb < 1048576 )); then
    err "  WARNING: Less than 1 GB free disk space on ${host} (${free_kb} KB available)."
    confirm "  Continue anyway?"
  else
    ok "  Disk space OK ($(( free_kb / 1024 )) MB free)"
  fi

  # 6. SpeechServer image present
  log "  Checking Docker images..."
  if ! ssh "${target}" 'docker image inspect sarah/speechserver:latest' &>/dev/null; then
    err "  Image sarah/speechserver:latest not found on ${host}. Run build-and-push.sh first."
    return 1
  fi
  ok "  SpeechServer image present"

  # 7. Main host reachable from speaker
  log "  Checking connectivity to main host (${PI_HOST})..."
  if ! ssh "${target}" "timeout 3 bash -c '</dev/tcp/${PI_HOST}/5672'" &>/dev/null; then
    err "  WARNING: Cannot reach ${PI_HOST}:5672 (RabbitMQ) from ${host}."
    err "  Ensure the main host is deployed and RabbitMQ is running."
    confirm "  Continue anyway?"
  else
    ok "  Main host ${PI_HOST} reachable from ${host}"
  fi

  # 8. Audio mixer controls for playback volume setup
  preflight_check_audio_controls "$host" "$target"

  ok "All pre-flight checks passed for ${host}."
  return 0
}

# ── Deploy to a single speaker ───────────────────────────────────────

deploy_speaker() {
  local host="$1"
  local target
  target="$(host_target "$host")"
  local env_source
  local playback_volume
  local playback_control
  local speaker_hostname

  if ! env_source="$(resolve_speaker_env "$host")"; then
    err "No speaker environment file found for ${host}."
    err "Create ${SCRIPT_DIR}/speaker/${host}.env or ${SCRIPT_DIR}/speaker/.env before deploying."
    exit 1
  fi

  log "Uploading compose files to ${target}:${DEPLOY_DIR}/"
  ssh "${target}" "mkdir -p ${DEPLOY_DIR}"
  scp "${SCRIPT_DIR}/speaker/docker-compose.yml" "${target}:${DEPLOY_DIR}/docker-compose.yml"
  scp "${SCRIPT_DIR}/speaker/asound.conf" "${target}:${DEPLOY_DIR}/asound.conf"

  if is_true "$FORCE_ENV_UPLOAD"; then
    scp "$env_source" "${target}:${DEPLOY_DIR}/.env"
    log "Refreshed remote .env on ${target}."
  elif ! ssh "${target}" "test -f ${DEPLOY_DIR}/.env"; then
    scp "$env_source" "${target}:${DEPLOY_DIR}/.env"
    log "Uploaded $(basename "$env_source") to ${target}."
  else
    ok ".env already exists on ${target} — not overwriting."
  fi

  # Preserve existing speaker settings while ensuring targeted speech messages
  # have a stable hostname to match against inside the container.
  speaker_hostname="$(get_env_value "$env_source" "SPEAKER_HOSTNAME" || true)"
  speaker_hostname="${speaker_hostname:-$host}"
  if ! ssh "${target}" "grep -q '^SPEAKER_HOSTNAME=' '${DEPLOY_DIR}/.env'"; then
    ssh "${target}" "printf '\\nSPEAKER_HOSTNAME=%s\\n' '${speaker_hostname}' >> '${DEPLOY_DIR}/.env'"
    log "Added missing SPEAKER_HOSTNAME=${speaker_hostname} to the existing remote .env."
  fi

  log "Starting SpeechServer on ${target}..."
  ssh "${target}" "cd ${DEPLOY_DIR} && docker compose up -d --force-recreate --remove-orphans"

  playback_volume="$(get_env_value "$env_source" "SPEAKER_PLAYBACK_VOLUME" || true)"
  playback_control="$(get_env_value "$env_source" "SPEAKER_PLAYBACK_CONTROL" || true)"
  if [[ -n "$playback_volume" ]]; then
    if [[ -n "$playback_control" ]]; then
      log "Applying playback volume on ${target}: ${playback_control}=${playback_volume}"
    else
      log "Applying playback volume on ${target}: auto-detect control (${playback_volume})"
    fi
    apply_playback_volume "$target" "$playback_volume" "$playback_control" || true
  fi

  ok "Deployment to ${target} complete."
}

# ── Main ─────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sarah Smart Home — Deploy SpeechServer to speakers"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  Main host:  ${PI_HOST}"
echo "  Speakers:   ${SPEAKERS}"
echo "  SSH user:   ${SSH_USER}"
echo "  Refresh env on target: ${FORCE_ENV_UPLOAD}"
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
