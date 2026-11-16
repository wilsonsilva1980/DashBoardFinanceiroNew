# Configuração de conexão da API via appsettings (PostgreSQL e SQL Server)

**Data:** 2026-11-16
**Ambiente:** Linux, .NET SDK 10.0.403 (pinned em `Projetos/global.json`), Docker Compose
**Projeto:** DashBoardFinanceiroNew (`Projetos/`)
**Arquivos alterados:** `Projetos/src/Financeiro.Api/appsettings.Development.json`, `Projetos/src/Financeiro.Infrastructure/Persistence/DatabaseConfiguration.cs`, `Projetos/README.md`

**Ver também:** [registro da sessão](./sessao-2026-11-16-conexao-api-postgres-sqlserver.md) · [subida do ambiente e E2E](./ambiente-desenvolvimento-subida-e2e.md) · [índice da documentação](./README.md)

## Objetivo

Fazer a API ler a conexão de banco dos arquivos `appsettings`, funcionando com os dois providers do Docker local
(PostgreSQL em `localhost:55432` e SQL Server em `localhost:51433`), sem exigir `export` de variáveis a cada terminal
e sem depender de user secrets.

## Contexto: erro original

Um `dotnet run` puro falhava na inicialização:

```
Unhandled exception. System.InvalidOperationException: Configure ConnectionStrings:Financeiro.
   at Financeiro.Infrastructure.Persistence.DatabaseConfiguration.AddFinancialDatabase(...)
   at Program.<Main>$(String[] args) in .../Financeiro.Api/Program.cs:line 22
```

Causa: `ConnectionStrings:Financeiro` não existia em nenhum `appsettings` versionado — a conexão só aparecia como `export`
manual no README. O banco estava saudável (container `healthy`, 4 migrations, ~50 tabelas), então era apenas configuração.
Uma primeira correção usou user secrets; ela foi **substituída** pela configuração em `appsettings` descrita abaixo
(o user secret foi removido com `dotnet user-secrets remove`).

## Como ficou

### 1. `appsettings.Development.json` — conexões dos dois providers

```json
{
  "Logging": { "...": "..." },
  "Database": { "Provider": "PostgreSql" },
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Port=55432;Database=financeiro;Username=financeiro;Password=<senha de infra/.env>",
    "SqlServer": "Server=localhost,51433;Database=financeiro;User Id=sa;Password=<senha de infra/.env>;Encrypt=True;TrustServerCertificate=True"
  }
}
```

As senhas são as mesmas de `Projetos/infra/.env` (que continua ignorado pelo Git) e servem somente para os containers locais
criados por `Projetos/infra/compose.yaml`.

### 2. `DatabaseConfiguration.cs` — resolução da conexão por provider

```csharp
var provider = configuration["Database:Provider"];
if (provider is not ("PostgreSql" or "SqlServer"))
    throw new InvalidOperationException("Database:Provider must be PostgreSql or SqlServer.");
var connection = ResolveConnectionString(configuration, provider);
```

```csharp
private static string ResolveConnectionString(IConfiguration configuration, string provider)
{
    // ConnectionStrings:Financeiro overrides the provider-specific entry (tests and explicit overrides).
    var connection = configuration.GetConnectionString("Financeiro");
    if (string.IsNullOrWhiteSpace(connection)) connection = configuration.GetConnectionString(provider);
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException(
            $"Configure ConnectionStrings:{provider} in appsettings.json/appsettings.Development.json, or set the ConnectionStrings__{provider} environment variable (ConnectionStrings:Financeiro keeps overriding both). See Projetos/README.md.");
    return connection;
}
```

Ordem de precedência final:

1. `ConnectionStrings:Financeiro` / `ConnectionStrings__Financeiro` (compatibilidade e testes de integração);
2. `ConnectionStrings:PostgreSql` ou `ConnectionStrings:SqlServer` conforme `Database:Provider`;
3. erro acionável se nenhuma existir.

A validação de `Database:Provider` passou para antes da conexão, para a mensagem de erro indicar a chave correta.

### 3. `README.md` — seção *Desenvolvimento* atualizada

Substituiu o passo a passo de `export` por:

```bash
dotnet run --project src/Financeiro.Api                              # PostgreSQL (default do appsettings)
Database__Provider=SqlServer dotnet run --project src/Financeiro.Api # SQL Server
```

Também documenta a precedência `ConnectionStrings__Financeiro` > `ConnectionStrings__<Provider>`, que as senhas do
`appsettings.Development.json` devem espelhar `infra/.env`, e que `Database=financeiro` no SQL Server é criado pelo migrator.

## Banco de dados (containers e migrations)

```bash
bash Projetos/scripts/dev/database.sh postgres    # financeiro-postgres-1  -> 127.0.0.1:55432 (healthy)
bash Projetos/scripts/dev/database.sh sqlserver   # financeiro-sqlserver-1 -> 127.0.0.1:51433 (healthy)
```

PostgreSQL já estava migrado. Para o SQL Server, o banco `financeiro` não existia, então foi aplicado:

```bash
export FINANCEIRO_MIGRATION_CONNECTION='Server=localhost,51433;Database=financeiro;User Id=sa;Password=<senha>;Encrypt=True;TrustServerCertificate=True'
bash Projetos/scripts/database/migrate.sh SqlServer Development
# Migrations applied: provider=SqlServer, environment=Development.
```

Estado final: SQL Server com **50 tabelas** e **4 migrations** em `__EFMigrationsHistory`.

## Validação executada

| Verificação | Comando | Resultado |
|---|---|---|
| Build da solução | `dotnet build Financeiro.slnx --no-restore` | `14 projects, 0 errors, 0 warnings` |
| Formatação | `dotnet format Financeiro.slnx --no-restore --verify-no-changes` | exit `0` |
| API com PostgreSQL | `ASPNETCORE_ENVIRONMENT=Development dotnet .../Financeiro.Api.dll --urls http://localhost:5196` | `/health` = `200` em 2s |
| API com SQL Server | `ASPNETCORE_ENVIRONMENT=Development Database__Provider=SqlServer dotnet .../Financeiro.Api.dll --urls http://localhost:5195` | `/health` = `200` em 2s |
| Conexão real ao banco (PostgreSQL) | `GET /api/v1/auth/csrf` + `POST /api/v1/auth/login` (e-mail inexistente), via HTTPS com cookie `__Host-` | `401` — consulta executada no banco, sem erro 500 |
| Conexão real ao banco (SQL Server) | mesmo probe com `Database__Provider=SqlServer` | `401` — consulta executada no banco, sem erro 500 |

> `/health` usa `AddHealthChecks()` sem verificação de banco, portanto sozinho não prova conectividade. Por isso o probe
> de login (que consulta `AspNetUsers` via Identity) foi usado como evidência real para cada provider.

## Ponto de atenção: segredos versionados

`appsettings.Development.json` **é versionado** (`git ls-files` confirma). Com esta configuração, as senhas locais dos
containers passaram a existir também no repositório. Mitigações disponíveis, sem mudar código:

- Sobrescrever por ambiente, mantendo o arquivo versionado apenas com valores locais:
  `ConnectionStrings__PostgreSql` / `ConnectionStrings__SqlServer` / `ConnectionStrings__Financeiro`;
- Se o repositório for compartilhado, trocar os valores por placeholders e distribuir as senhas por `.env`, user secrets
  ou variáveis de ambiente (o código já aceita as três formas).

## Como alternar de provider

```bash
# PostgreSQL (padrão)
dotnet run --project src/Financeiro.Api

# SQL Server, sem editar arquivos
Database__Provider=SqlServer dotnet run --project src/Financeiro.Api

# Fixar um provider no arquivo
# appsettings.Development.json -> "Database": { "Provider": "SqlServer" }
```

Requisitos para o SQL Server local: container iniciado com `scripts/dev/database.sh sqlserver` e migrations aplicadas
(`migrate.sh SqlServer Development`) para o banco `financeiro`.
