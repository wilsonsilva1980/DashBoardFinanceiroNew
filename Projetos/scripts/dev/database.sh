#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
provider="${1:?Usage: database.sh postgres|sqlserver}"
case "$provider" in postgres|sqlserver) ;; *) echo "Invalid provider" >&2; exit 2 ;; esac
cd "$project_root/infra"
docker compose --env-file images.env --env-file .env --profile "$provider" up -d --wait "$provider"
