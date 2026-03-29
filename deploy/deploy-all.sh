#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────────────
# deploy-all.sh — Full deployment: build → push → deploy pi → deploy speakers
# ────────────────────────────────────────────────────────────────────
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export PI_HOST="${PI_HOST:-pi}"
export SPEAKERS="${SPEAKERS:-speaker1 speaker3}"
export REGISTRY="${REGISTRY:-sarah}"
export TAG="${TAG:-latest}"
export PLATFORM="${PLATFORM:-linux/amd64}"
export SSH_USER="${SSH_USER:-pi}"

# ── Helpers ──────────────────────────────────────────────────────────

log()  { echo -e "\033[1;34m>>>\033[0m $*"; }
ok()   { echo -e "\033[1;32m✓\033[0m $*"; }

confirm() {
  local msg="${1:-Continue?}"
  read -rp "$msg [y/N] " answer
  [[ "$answer" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }
}

# ── Main ─────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sarah Smart Home — Full Deployment"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  Platform:  ${PLATFORM}"
echo "  Registry:  ${REGISTRY}"
echo "  Tag:       ${TAG}"
echo "  Pi host:   ${PI_HOST}"
echo "  Speakers:  ${SPEAKERS}"
echo "  SSH user:  ${SSH_USER}"
echo ""
echo "Steps:"
echo "  1. Build all Docker images"
echo "  2. Transfer images to target hosts"
echo "  3. Deploy main stack to ${PI_HOST}"
echo "  4. Deploy SpeechServer to speakers (${SPEAKERS})"
echo ""

confirm "Start full deployment?"

echo ""
echo "────────── Step 1+2: Build & Transfer ──────────"
"${SCRIPT_DIR}/build-and-push.sh"

echo ""
echo "────────── Step 3: Deploy to ${PI_HOST} ──────────"
"${SCRIPT_DIR}/deploy-pi.sh"

echo ""
echo "────────── Step 4: Deploy to speakers ──────────"
"${SCRIPT_DIR}/deploy-speakers.sh"

echo ""
echo "═══════════════════════════════════════════════════════════════"
ok "Full deployment complete!"
echo ""
echo "  Frontend:    http://${PI_HOST}:8081"
echo "  Keycloak:    http://${PI_HOST}:8080"
echo "  RabbitMQ:    http://${PI_HOST}:15672"
echo ""
echo "  SpeechServer endpoints:"
for speaker in $SPEAKERS; do
  echo "    ${speaker}:  http://${speaker}:5008"
done
echo ""
