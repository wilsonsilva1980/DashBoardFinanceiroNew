#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
: "${FINANCEIRO_TEST_PROVIDER:?Set PostgreSql or SqlServer}"
: "${FINANCEIRO_TEST_CONNECTION:?Set database connection string}"
case "$FINANCEIRO_TEST_PROVIDER" in PostgreSql|SqlServer) ;; *) echo "Invalid provider" >&2; exit 2 ;; esac
cd "$project_root"
dotnet test tests/Financeiro.Infrastructure.IntegrationTests --no-restore --configuration Release
dotnet test tests/Financeiro.Api.IntegrationTests --no-restore --configuration Release
dotnet test tests/Financeiro.Migrations.Tests --no-restore --configuration Release
