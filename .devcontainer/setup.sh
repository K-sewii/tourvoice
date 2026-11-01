#!/usr/bin/env bash
# Tao .env voi secret ngau nhien + origin cong khai cua web trong GitHub Codespaces,
# de chi can `docker compose up --build` la demo duoc.
set -euo pipefail

cd "$(dirname "$0")/.."

if [ ! -f .env ]; then
  cp .env.example .env
  echo "Da tao .env tu .env.example"
fi

rand() { LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c "${1:-40}"; }

# Chi thay gia tri mau, giu nguyen cau hinh nguoi dung da sua.
replace_placeholder() {
  local key="$1" placeholder="$2" value="$3"
  if grep -qE "^${key}=${placeholder}$" .env; then
    sed -i "s|^${key}=.*|${key}=${value}|" .env
  fi
}

replace_placeholder DB_PASSWORD 'change-me-password' "$(rand 24)"
replace_placeholder DB_ROOT_PASSWORD 'change-me-root' "$(rand 24)"
replace_placeholder JWT_KEY 'change-me-to-a-random-secret-at-least-32-characters' "$(rand 48)"

# Cong 5173 duoc Codespaces forward ra domain cong khai: https://<codespace>-5173.<domain>
if [ -n "${CODESPACE_NAME:-}" ] && [ -n "${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-}" ]; then
  origin="https://${CODESPACE_NAME}-5173.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
  sed -i "s|^WEB_ORIGIN=.*|WEB_ORIGIN=${origin}|" .env
  echo "WEB_ORIGIN=${origin}"
fi

cat <<'EOF'

Xong. Chay:
  docker compose up --build -d
  docker compose ps

Web: tab Ports -> cong 5173 -> Open in Browser (nho dat visibility = Public de nguoi khac demo duoc).
EOF
