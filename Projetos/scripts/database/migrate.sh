#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
provider="${1:?Usage: migrate.sh PostgreSql|SqlServer Development|Test|Staging|Production}"
environment="${2:?Specify target environment}"
case "$provider" in PostgreSql|SqlServer) ;; *) echo "Invalid provider" >&2; exit 2 ;; esac
case "$environment" in Development|Test|Staging|Production) ;; *) echo "Invalid environment" >&2; exit 2 ;; esac
: "${FINANCEIRO_MIGRATION_CONNECTION:?Set target connection}"
cd "$project_root"
dotnet run --project src/Financeiro.Migrator --configuration Release --no-launch-profile -- "$provider" "$environment"
