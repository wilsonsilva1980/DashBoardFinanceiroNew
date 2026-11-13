#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
workspace_root="$(dirname -- "$project_root")"
validation_root="$(mktemp -d -t financeiro-clean-XXXXXXXX)"
mkdir "$validation_root/source"
# Snapshot only application and workflow source. Git ignore rules omit local secrets and outputs.
python3 - "$workspace_root" "$validation_root/source" <<'PY'
from pathlib import Path
import shutil, subprocess, sys
source, target = map(Path, sys.argv[1:])
files = subprocess.check_output(['git','-C',str(source),'ls-files','-z','--cached','--others','--exclude-standard','--','Projetos','.github/workflows/financeiro-ci.yml']).split(b'\0')
for relative in set(files):
    if not relative:
        continue
    path = Path(relative.decode())
    if '.env' == path.name or any(part in {'bin','obj','node_modules','dist','artifacts','.git'} for part in path.parts):
        continue
    destination = target/path
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source/path, destination)
PY
git -C "$validation_root/source" init --initial-branch=main --quiet
git -C "$validation_root/source" add .
git -C "$validation_root/source" -c user.name='Local CI validation' -c user.email='ci@localhost' commit --quiet -m 'Temporary source snapshot'
git clone --quiet --no-hardlinks "$validation_root/source" "$validation_root/checkout"
cd "$validation_root/checkout/Projetos"
bash scripts/ci/check.sh backend
bash scripts/ci/check.sh frontend
bash scripts/ci/provider.sh PostgreSql
bash scripts/ci/provider.sh SqlServer
printf 'Clean checkout validation passed: %s\n' "$validation_root/checkout"
