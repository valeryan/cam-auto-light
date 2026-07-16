#!/bin/bash
# Ensure route to 192.168.3.0/24 via 192.168.4.1
# If the route is updated, restart the CamAutoLight service (label: com.camautolight).

set -euo pipefail

IOT_NET="192.168.3.0/24"
IOT_GW="192.168.4.1"
TEST_HOST="192.168.3.3"   # any host in the subnet is fine

# Rate-limit restarts to avoid flapping
RESTART_MIN_INTERVAL=20
STAMPFILE="/var/run/ensure-iot-route.lastrestart"

log() { logger -t ensure-iot-route "$*"; echo "$(date '+%F %T') $*"; }

sleep 3  # let VPN settle

route_changed=0
current_gw="$(/sbin/route -n get "${TEST_HOST}" 2>/dev/null | awk '/gateway:/{print $2}' || true)"

if [[ "${current_gw:-}" == "${IOT_GW}" ]]; then
  log "Route for ${IOT_NET} already via ${IOT_GW}; nothing to do."
else
  if [[ -n "${current_gw:-}" && "${current_gw}" != "${IOT_GW}" ]]; then
    log "Existing route for ${IOT_NET} via ${current_gw}; deleting…"
    /sbin/route -n delete -net "${IOT_NET}" || true
  fi
  log "Adding route: ${IOT_NET} via ${IOT_GW}"
  /sbin/route -n add -net "${IOT_NET}" "${IOT_GW}"
  route_changed=1
fi

# Restart com.camautolight if route changed
if [[ "${route_changed}" -eq 1 ]]; then
  # Determine if it's a per-user LaunchAgent or a system LaunchDaemon
  CONSOLE_USER="$(/usr/bin/stat -f%Su /dev/console)"
  CONSOLE_UID="$(id -u "${CONSOLE_USER}")"

  # Rate limit
  now=$(date +%s)
  last=0
  [[ -f "${STAMPFILE}" ]] && last=$(cat "${STAMPFILE}" 2>/dev/null || echo 0)

  if (( now - last >= RESTART_MIN_INTERVAL )); then
    # Try user agent first (most camera helpers run as a LaunchAgent)
    if /bin/launchctl print "gui/${CONSOLE_UID}/com.camautolight" >/dev/null 2>&1; then
      log "Route changed; kickstarting user agent gui/${CONSOLE_UID}/com.camautolight"
      /bin/launchctl kickstart -k "gui/${CONSOLE_UID}/com.camautolight" && log "camautolight restarted (user agent)."
      echo "${now}" > "${STAMPFILE}" || true
    # Fallback: system domain
    elif /bin/launchctl print "system/com.camautolight" >/dev/null 2>&1; then
      log "Route changed; kickstarting system daemon system/com.camautolight"
      /bin/launchctl kickstart -k "system/com.camautolight" && log "camautolight restarted (system daemon)."
      echo "${now}" > "${STAMPFILE}" || true
    else
      log "com.camautolight not found in gui/${CONSOLE_UID} or system domains; no restart performed."
    fi
  else
    log "Route changed but restart suppressed (last < ${RESTART_MIN_INTERVAL}s)."
  fi
fi
