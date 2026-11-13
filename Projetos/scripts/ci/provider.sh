#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
provider="${1:?Usage: provider.sh PostgreSql|SqlServer}"
case "$provider" in PostgreSql) service=postgres ;; SqlServer) service=sqlserver ;; *) echo "Invalid provider" >&2; exit 2 ;; esac
command -v docker >/dev/null
command -v dotnet >/dev/null
# Every invocation owns a unique disposable Compose project and random host ports.
project_name="financeiro-ci-${RANDOM}-$$"
env_file="$(mktemp)"
chmod 600 "$env_file"
postgres_password="F1!$(head -c 24 /dev/urandom | base64)"
sqlserver_password="F1!$(head -c 24 /dev/urandom | base64)"
printf 'POSTGRES_PASSWORD=%s\nSQLSERVER_PASSWORD=%s\nPOSTGRES_PORT=0\nSQLSERVER_PORT=0\n' "$postgres_password" "$sqlserver_password" > "$env_file"
compose=(docker compose --project-name "$project_name" --file "$project_root/infra/compose.yaml" --env-file "$project_root/infra/images.env" --env-file "$env_file" --profile "$service")
cleanup() {
  "${compose[@]}" down --volumes >/dev/null 2>&1 || true
  rm -f -- "$env_file"
}
trap cleanup EXIT
"${compose[@]}" up --detach --wait "$service"
if [[ "$provider" == PostgreSql ]]; then
  endpoint="$("${compose[@]}" port postgres 5432)"
  export FINANCEIRO_TEST_CONNECTION="Host=127.0.0.1;Port=${endpoint##*:};Database=financeiro;Username=financeiro;Password=$postgres_password"
else
  endpoint="$("${compose[@]}" port sqlserver 1433)"
  export FINANCEIRO_TEST_CONNECTION="Server=127.0.0.1,${endpoint##*:};Database=master;User Id=sa;Password=$sqlserver_password;Encrypt=True;TrustServerCertificate=True"
fi
export FINANCEIRO_TEST_PROVIDER="$provider"
cd "$project_root"
dotnet restore Financeiro.slnx --locked-mode
bash scripts/test/provider.sh
