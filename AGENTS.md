# AGENTS.md

Guidance for coding agents working in this repository.

## Repository layout

- `Projetos/` — main implementation (all app code, scripts, tests live here)
  - `src/` — .NET backend (solution: `Financeiro.slnx`, SDK pinned in `global.json`)
  - `frontend/` — React/Vite/TypeScript client (`package.json`, `package-lock.json`)
  - `tests/` — .NET tests (e.g. `Financeiro.Migrations.Tests`)
  - `scripts/` — `build/`, `dev/`, `ci/`, `test/`, `database/` helper scripts
  - `infra/` — local dev containers/config (`.env.example`, images)
- `Guia/` — business/architecture reference docs (pt-BR) and source spreadsheet
- `openspec/` — OpenSpec config and change artifacts (`openspec/changes/...`)
- `docs/`, `prompts/` — supporting documentation/prompt history

Prefer reading `Projetos/README.md` and `Guia/Arquitetura_Sistema_Financeiro_NovaPlanilhaDividas2_v4_Consolidada_SaaS.md` for domain/architecture context before changing behavior.

## Prerequisites & pinned toolchain

- .NET SDK `10.0.403` (`Projetos/global.json`, `rollForward: disable`)
- Node `26.10.0`, npm `12.2.0` (`Projetos/frontend/package.json` engines/packageManager, `.nvmrc`)
- Docker + Compose for local databases
- Bash for repo scripts

## Common commands

Run from the paths noted; scripts assume Bash.

### Build
- Reproducible build (from repo root): `bash Projetos/scripts/build/build.sh`

### Backend (from `Projetos/`)
- Restore: `dotnet tool restore && dotnet restore Financeiro.slnx --locked-mode`
- Format check: `dotnet format Financeiro.slnx --no-restore --verify-no-changes`
- Build: `dotnet build Financeiro.slnx --no-restore --configuration Release`
- Unit/non-DB tests: `dotnet test Financeiro.slnx --no-build --no-restore --configuration Release --filter 'Category!=Database'`
- CI-equivalent backend check: `bash scripts/ci/check.sh backend`

### Frontend (from `Projetos/frontend/` or via script in `Projetos/`)
- Install: `npm ci`
- Build: `npm run build` (runs `tsc -b && vite build`)
- Unit tests (Vitest): `npm test`
- Browser/e2e tests (Playwright): `npm run test:browser`
- Dev server: `npm run dev`
- CI-equivalent frontend check (from `Projetos/`): `bash scripts/ci/check.sh frontend`

### Database / migrations (from `Projetos/`)
- Start local DB: `bash scripts/dev/database.sh postgres` or `bash scripts/dev/database.sh sqlserver`
- Apply migrations: set `FINANCEIRO_MIGRATION_CONNECTION`, then `bash scripts/database/migrate.sh PostgreSql Development` (or `SqlServer`)
- Provider DB/integration tests: set `FINANCEIRO_TEST_PROVIDER` and `FINANCEIRO_TEST_CONNECTION`, then `bash scripts/test/provider.sh`

### Full CI-like verification (from repo root, requires .NET + Node/npm)
- `bash Projetos/scripts/ci/verify-clean-checkout.sh` (runs checks from a clean snapshot of `Projetos/` + workflow)

## Conventions

- **Formatting**: `.editorconfig` at `Projetos/` — UTF-8, LF, final newline, spaces; 2-space indent default, 4-space for `*.cs`.
- **.NET**: `Nullable` + `ImplicitUsings` enabled, warnings as errors, locked restore, deterministic builds (`Projetos/Directory.Build.props`). Keep migrations/provider-specific concerns aligned with existing dual-provider setup (PostgreSQL & SQL Server assemblies/snapshots).
- **Frontend**: TypeScript + React 19 + Vite; forms/validation patterns use React Hook Form + Zod; data fetching via TanStack Query (follow existing feature structure under `src/features/`, shared utilities under `src/shared/`).
- **API/security**: first-party cookie auth with CSRF (`__Host-` cookies, `X-CSRF-TOKEN` on mutations), strict context/tenant-space scoping — preserve these invariants and consult `Projetos/README.md` before changing auth/context flows.
- **Change workflow**: if a task involves planned/spec-driven work, check `openspec/` and relevant skills/commands (`opsx-*`) before large changes.
- **Scope hygiene**: avoid modifying generated/build artifacts (`dist/`, `bin/`, `obj/`, `node_modules/`, Playwright browser caches under `Projetos/artifacts/`) unless explicitly required.

## Notes

- CI is defined in `.github/workflows/financeiro-ci.yml` (backend, frontend, migration/integration matrix for PostgreSQL and SQL Server).
- **Local database config**: dev connection strings live in `Projetos/src/Financeiro.Api/appsettings.Development.json` (`ConnectionStrings:PostgreSql` / `ConnectionStrings:SqlServer`, selected by `Database:Provider`). Switch provider with `Database__Provider=SqlServer`; override a connection with `ConnectionStrings__<Provider>` (or `ConnectionStrings__Financeiro`). See `docs/configuracao-conexao-api-postgres-sqlserver.md`.
- There is no separate root `package.json`; frontend and backend workflows are rooted in `Projetos/`.
- No Cursor/Copilot instruction files were found at init time; update this file if project conventions or commands change.
