#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_root"
dotnet restore Financeiro.slnx --locked-mode
dotnet build Financeiro.slnx --no-restore --configuration Release
npm --prefix frontend ci
npm --prefix frontend run build
