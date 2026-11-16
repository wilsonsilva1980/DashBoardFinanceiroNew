# Subida do ambiente de desenvolvimento e validação E2E

**Data:** 2026-11-16
**Ambiente:** Linux, .NET SDK 10.0.403 (pinned em `Projetos/global.json`), Node 26.10.0, npm 12.2.0, Docker 29.8.2 / Compose 5.6.0
**Projeto:** DashBoardFinanceiroNew (`Projetos/`)
**Banco escolhido:** PostgreSQL (também existe volume de SQL Server criado anteriormente)

> **Atualização (2026-11-16):** a API passou a ler a conexão de banco de `src/Financeiro.Api/appsettings.Development.json`
> (PostgreSQL e SQL Server), sem `export` de variáveis. A seção *3. API* foi ajustada; o histórico da subida original está
> preservado na seção *Validação E2E do fluxo de autenticação*.

## Objetivo

Subir a stack local (`PostgreSQL` + API .NET + frontend Vite), validar o fluxo de autenticação/contexto
ponta a ponta e remover o aviso de certificado HTTPS do ambiente de desenvolvimento.

## Estado final (serviços no ar)

| Serviço | Endereço | Status |
|---|---|---|
| PostgreSQL (container `financeiro-postgres-1`) | `localhost:55432` (bind em 127.0.0.1) | healthy |
| API `Financeiro.Api` | `https://localhost:7264` e `http://localhost:5198` | rodando |
| Frontend Vite (React) | `http://localhost:5173` | rodando |
| Swagger (somente Development) | `https://localhost:7264/swagger/index.html` | disponível |
| OpenAPI | `https://localhost:7264/openapi/v1.json` | disponível |

Logs desta sessão:

- API: `/tmp/opencode/financeiro-logs/api.log`
- Frontend: `/tmp/opencode/financeiro-logs/frontend.log`
- Script E2E: `/tmp/opencode/e2e-auth.sh`

> Logs e script estão em `/tmp` (voláteis). Se precisarem ser preservados, copie para `Projetos/artifacts/`.

## Passo a passo executado

### 1. Pré-checagem do toolchain

```bash
dotnet --list-sdks      # 10.0.403 presente (rollForward: disable respeitado)
node --version          # v26.10.0
npm --version           # 12.2.0
docker compose version  # 5.6.0
```

`Projetos/frontend/node_modules` já existia; não foi necessário `npm ci`.

### 2. Banco (PostgreSQL)

```bash
bash Projetos/scripts/dev/database.sh postgres
# Container financeiro-postgres-1 Started / Healthy
```

O script usa `Projetos/infra/compose.yaml` com `--env-file images.env --env-file .env`
(senha vem de `Projetos/infra/.env`, que é ignorado pelo Git).

Estado do banco no início: schema já migrado (**50 tabelas**), `AspNetUsers` vazia.

### 3. API

```bash
cd Projetos
nohup dotnet run --project src/Financeiro.Api --launch-profile https \
  > /tmp/opencode/financeiro-logs/api.log 2>&1 &
```

A conexão com o banco **não** precisa mais ser exportada: em Development ela vem de
`src/Financeiro.Api/appsettings.Development.json` (`ConnectionStrings:PostgreSql` / `ConnectionStrings:SqlServer`,
selecionada por `Database:Provider`). O perfil `https` já define `ASPNETCORE_ENVIRONMENT=Development` e as URLs
`https://localhost:7264;http://localhost:5198`.

Para usar SQL Server em vez de PostgreSQL:

```bash
Database__Provider=SqlServer nohup dotnet run --project src/Financeiro.Api --launch-profile https \
  > /tmp/opencode/financeiro-logs/api.log 2>&1 &
```

Detalhes e precedência de configuração: [`configuracao-conexao-api-postgres-sqlserver.md`](./configuracao-conexao-api-postgres-sqlserver.md).

> Histórico: a subida original desta sessão usou `--no-launch-profile` com `export ConnectionStrings__Financeiro=...`
> porque as conexões ainda não existiam nos `appsettings`. Isso foi substituído pela configuração em `appsettings`.

### 4. Frontend

```bash
cd Projetos/frontend
nohup npm run dev > /tmp/opencode/financeiro-logs/frontend.log 2>&1 &
```

O proxy do Vite (`vite.config.ts`) encaminha `/api` para `process.env.FINANCEIRO_API_URL`
ou, por padrão, `https://localhost:7264` com `secure: false`. Como a API precisa de HTTPS
(ver achado abaixo), **não** defina `FINANCEIRO_API_URL=http://localhost:5000`.

## Achado importante: a API exige HTTPS

Tentar subir a API em HTTP puro (`--urls http://localhost:5000`) faz `GET /api/v1/auth/csrf`
falhar com **500**:

```
warn: Financeiro.Api.Security.ApiExceptionHandler[0]
      Request failed with InvalidOperationException 500 ... /api/v1/auth/csrf
```

Causa: `Projetos/src/Financeiro.Api/Security/ApiSecurityRegistration.cs` configura
antiforgery e cookies de autenticação com `CookieSecurePolicy.Always` e nomes `__Host-`
(`__Host-financeiro-auth`, `__Host-financeiro-csrf`). A geração do token antiforgery exige
requisição HTTPS. Sob HTTPS o endpoint responde 200 normalmente.

Consequências práticas:

- Use a API em `https://localhost:7264` (e o proxy do Vite para ela).
- O navegador acessa o app em `http://localhost:5173`; `localhost` é origem confiável, então
  cookies `Secure` + `SameSite=Strict` funcionam.
- Não "conserte" isso relaxando `SecurePolicy` — é um invariante de segurança do projeto.

## Certificado de desenvolvimento (`dotnet dev-certs`)

```bash
dotnet dev-certs https --trust
# exit 4: "trusted by some clients but not by others"
```

No Linux o `--trust` só cria a âncora em `~/.aspnet/dev-certs/trust/` e avisa que ela só é
usada se `SSL_CERT_DIR` apontar para lá. Para navegadores, a âncora precisa ir ao banco NSS
do usuário (usado por Chrome/Edge/Brave no Linux):

```bash
certutil -d sql:"$HOME/.pki/nssdb" -A -t "P,," \
  -n "ASP.NET Core HTTPS development certificate" \
  -i "$HOME/.aspnet/dev-certs/trust/aspnetcore-localhost-2CC4425D74D84DF1E1A60BFBCFD8C19A6C1051D4.pem"
```

Verificação real (Chrome headless buscou a API sem erro de certificado):

```bash
/opt/google/chrome/chrome --headless=new --disable-gpu --no-first-run \
  --virtual-time-budget=6000 --dump-dom https://localhost:7264/health
# => <pre>Healthy</pre>
```

Para clientes OpenSSL (`curl`, `wget`, clients .NET) foi adicionado ao final de `~/.bashrc`:

```bash
# ASP.NET Core HTTPS dev certificate (dotnet dev-certs https --trust no Linux)
if [ -d "$HOME/.aspnet/dev-certs/trust" ]; then
  case ":$SSL_CERT_DIR:" in
    *":$HOME/.aspnet/dev-certs/trust:"*) ;;
    *) export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:${SSL_CERT_DIR:-/etc/ssl/certs}" ;;
  esac
fi
```

Resultado: `curl https://localhost:7264/health` → `Healthy` (200) sem `-k` em shell interativo novo.

Limitações conhecidas:

- `bash -lc` não carrega `~/.bashrc`; só shells **interativos** herdam a variável.
- **Node** usa CA bundle próprio e não lê `SSL_CERT_DIR` — o proxy do Vite precisa continuar com `secure: false`.
- **Firefox** ainda sem perfil criado (`~/.mozilla/firefox` não existe); ao abrir a primeira vez,
  é preciso importar o `.pem` no perfil (ou habilitar `security.enterprise_roots`).
- Instalação system-wide exigiria `sudo` + `/usr/local/share/ca-certificates/` + `update-ca-certificates`.

## Validação E2E do fluxo de autenticação

Script `/tmp/opencode/e2e-auth.sh` (usa cookie jar, exige `X-CSRF-TOKEN` nas mutações e
renova o token após login/logout, como o README determina):

```bash
#!/usr/bin/env bash
set -uo pipefail
BASE="${BASE:-https://localhost:7264}"
JAR=/tmp/opencode/e2e-cookies.txt
EMAIL="e2e.teste.$(date +%s)@example.com"
PASSWORD='TesteE2E!2026abc'

refresh_csrf() { CSRF=$(curl -sk -b "$JAR" -c "$JAR" "$BASE/api/v1/auth/csrf" | jq -r .token); }

refresh_csrf
# POST /api/v1/auth/register  {email,password,displayName}         -> 201
# POST /api/v1/auth/register  (duplicado)                          -> 400
# refresh_csrf; POST /api/v1/auth/login {email,password}           -> 204
# GET  /api/v1/auth/me                                             -> 200 (tenantId null)
# POST /api/v1/context/onboarding {tenantName,spaceName,currency,requestKey} -> 200
# GET  /api/v1/context/spaces                                      -> 200
# GET  /api/v1/auth/me                                             -> 200 (contexto ativo)
# GET  /api/v1/dashboard/resumo?from=...&until=...&currency=BRL    -> 200
# refresh_csrf; POST /api/v1/auth/logout                           -> 204
# GET  /api/v1/auth/me                                             -> 401
```

### Resultado: todos os passos passaram

| Passo | Status | Observação |
|---|---|---|
| `GET /api/v1/auth/csrf` | 200 | cookie `__Host-financeiro-csrf` (`secure; samesite=strict; httponly`) |
| `POST /api/v1/auth/register` | 201 | usuário criado |
| `POST /api/v1/auth/register` (duplicado) | 400 | `DuplicateUserName` — validação do Identity OK |
| `POST /api/v1/auth/login` | 204 | sessão por cookie `__Host-financeiro-auth` |
| `GET /api/v1/auth/me` (pré-onboarding) | 200 | `tenantId: null`, `spaceId: null` |
| `POST /api/v1/context/onboarding` | 200 | tenant + espaço criados |
| `GET /api/v1/context/spaces` | 200 | `Espaco E2E` / BRL |
| `GET /api/v1/auth/me` (pós-onboarding) | 200 | contexto ativo no retorno |
| `GET /api/v1/dashboard/resumo` | 200 | totais zerados, sem erro |
| `POST /api/v1/auth/logout` | 204 | sessão revogada no banco |
| `GET /api/v1/auth/me` (pós-logout) | 401 | cookie antigo não recupera acesso |

Também foi validado o caminho do navegador (via proxy do Vite): `http://localhost:5173/api/v1/auth/csrf`
responde 200, e `https://localhost:7264/health` responde `Healthy`.

### Dados de teste deixados no banco local (descartáveis)

- Usuário: `e2e.teste.1794840366@example.com` / `TesteE2E!2026abc` (conta **local**, criada só para o teste)
- Tenant: `Tenant E2E` — `48cd80be-871d-4cbf-9e11-bc10bdc7385a`
- Espaço: `Espaco E2E` (`BRL`) — `10363d2f-9610-4f8b-8905-44391d32089b`
- Tabelas do domínio usam `snake_case` (`AspNetUsers`, `tenants`, `membros_tenant`, ...)

Para remover o resíduo do teste, o caminho correto é apagar o tenant/usuário pelo banco local
(ou recriar o volume, apenas se não houver dados relevantes):

```bash
docker exec -it financeiro-postgres-1 psql -U financeiro -d financeiro
-- DELETE FROM tenants WHERE id = '48cd80be-871d-4cbf-9e11-bc10bdc7385a';
-- DELETE FROM "AspNetUsers" WHERE email = 'e2e.teste.1794840366@example.com';
```

## Como encerrar o ambiente

```bash
pkill -f Financeiro.Api
pkill -f vite
docker compose -f Projetos/infra/compose.yaml \
  --env-file Projetos/infra/images.env --env-file Projetos/infra/.env \
  --profile postgres down
```

Não use `down -v`: os volumes são persistentes e servem para trocar de provider sem perder dados.

## Próximos passos sugeridos

1. Rodar `bash Projetos/scripts/ci/check.sh frontend` e `... backend` (ou `scripts/ci/verify-clean-checkout.sh`) antes de commit.
2. Importar o certificado no Firefox quando o perfil existir.
3. Aplicar migrations novas com `bash Projetos/scripts/database/migrate.sh PostgreSql Development` (exige `FINANCEIRO_MIGRATION_CONNECTION`).
4. Se for testar também SQL Server: `bash Projetos/scripts/dev/database.sh sqlserver` e trocar `Database__Provider`/connection string — um provider por vez.
