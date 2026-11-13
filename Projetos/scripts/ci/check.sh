#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_root"
case "${1:?Usage: check.sh backend|frontend}" in
  backend)
    dotnet tool restore
    dotnet restore Financeiro.slnx --locked-mode
    dotnet format Financeiro.slnx --no-restore --verify-no-changes
    dotnet build Financeiro.slnx --no-restore --configuration Release
    dotnet test Financeiro.slnx --no-build --no-restore --configuration Release --filter 'Category!=Database'
    ;;
  frontend)
    npm --prefix frontend ci
    npm --prefix frontend run build
    npm --prefix frontend test
    ;;
  *) echo "Invalid check" >&2; exit 2 ;;
esac
