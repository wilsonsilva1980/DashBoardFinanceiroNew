# Sessão 2026-11-16 — conexão da API com PostgreSQL e SQL Server via appsettings

**Data:** 2026-11-16
**Ambiente:** Linux, .NET SDK 10.0.403 (pinned em `Projetos/global.json`), Docker Compose
(`financeiro-postgres-1` em `127.0.0.1:55432`, `financeiro-sqlserver-1` em `127.0.0.1:51433`)
**Repositório:** DashBoardFinanceiroNew
**Escopo:** destravar a inicialização da API e configurar a connection string nos arquivos `appsettings` para os dois
providers do Docker local.

**Ver também:** [referência da configuração](./configuracao-conexao-api-postgres-sqlserver.md) · [subida do ambiente e E2E](./ambiente-desenvolvimento-subida-e2e.md) · [índice da documentação](./README.md)

## 1. Situação inicial

`dotnet run` em `Projetos/src/Financeiro.Api` compilava e quebrava na inicialização:

```
Unhandled exception. System.InvalidOperationException: Configure ConnectionStrings:Financeiro.
   at Financeiro.Infrastructure.Persistence.DatabaseConfiguration.AddFinancialDatabase(...)
      in .../DatabaseConfiguration.cs:line 16
   at Program.<Main>$(String[] args) in .../Financeiro.Api/Program.cs:line 22
```

Diagnóstico: `DatabaseConfiguration.AddFinancialDatabase` (chamado em `Program.cs:22`) exige `Database:Provider` **e**
`ConnectionStrings:Financeiro`. O provider existia (`appsettings.json` → `PostgreSql`), a conexão não existia em nenhum
arquivo versionado — só como `export` manual no README. Hipótese de banco parado foi descartada: container `healthy`,
banco `financeiro` com 4 migrations e ~50 tabelas.

## 2. Cronologia das correções

| Etapa | Ação | Resultado |
|---|---|---|
| 1 | `UserSecretsId` no `.csproj` + `dotnet user-secrets set "ConnectionStrings:Financeiro"` | `dotnet run` passou a subir (`/health` = 200) |
| 2 | Mensagem de erro do `DatabaseConfiguration` tornou-se acionável | erro passou a apontar as opções de configuração |
| 3 | (substituição da etapa 1) conexões movidas para `appsettings.Development.json`, user secret removido | API lê a conexão do appsettings, sem depender de segredo local |
| 4 | Resolução de conexão por provider em `DatabaseConfiguration` | um arquivo serve PostgreSQL e SQL Server |
| 5 | Banco `financeiro` criado/migrado no SQL Server | 50 tabelas / 4 migrations |
| 6 | `README.md` reescrito na seção *Desenvolvimento* | comandos atuais documentados |

O user secret criado na etapa 1 foi removido (`dotnet user-secrets remove`), porque em Development ele tem precedência
sobre `appsettings.Development.json` e mascararia a nova configuração (inclusive quebraria a troca para SQL Server).

## 3. Estado final da configuração

### `Projetos/src/Financeiro.Api/appsettings.Development.json`

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "Database": { "Provider": "PostgreSql" },
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Port=55432;Database=financeiro;Username=financeiro;Password=<infra/.env>",
    "SqlServer": "Server=localhost,51433;Database=financeiro;User Id=sa;Password=<infra/.env>;Encrypt=True;TrustServerCertificate=True"
  }
}
```

`Projetos/src/Financeiro.Api/appsettings.json` segue com apenas `Database:Provider = PostgreSql` (nenhum segredo em
arquivo base). As senhas espelham `Projetos/infra/.env`, que continua ignorado pelo Git.

### `Projetos/src/Financeiro.Infrastructure/Persistence/DatabaseConfiguration.cs`

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

Precedência: `ConnectionStrings:Financeiro` (`ConnectionStrings__Financeiro`) → `ConnectionStrings:<PostgreSql|SqlServer>`
(`ConnectionStrings__PostgreSql` / `ConnectionStrings__SqlServer`) → exceção com instrução. A chave `Financeiro` foi
mantida primeiro para não quebrar os testes de integração, que injetam a conexão via
`UseSetting("ConnectionStrings:Financeiro", ...)` em `tests/Financeiro.Api.IntegrationTests/TestApi.cs`.

### `Projetos/src/Financeiro.Api/Financeiro.Api.csproj`

```xml
<UserSecretsId>66ee6c70-740c-41b7-8556-10d657809f01</UserSecretsId>
```

Mantido como alternativa opcional de segredo local (nenhum segredo gravado atualmente).

## 4. Como usar

```bash
# containers (um por vez ou ambos; portas distintas)
bash Projetos/scripts/dev/database.sh postgres
bash Projetos/scripts/dev/database.sh sqlserver

# API com PostgreSQL (padrão do appsettings)
dotnet run --project Projetos/src/Financeiro.Api

# API com SQL Server, sem editar arquivo
Database__Provider=SqlServer dotnet run --project Projetos/src/Financeiro.Api

# migrations (SQL Server já aplicado nesta sessão)
export FINANCEIRO_MIGRATION_CONNECTION='Server=localhost,51433;Database=financeiro;User Id=sa;Password=<senha>;Encrypt=True;TrustServerCertificate=True'
bash Projetos/scripts/database/migrate.sh SqlServer Development
```

## 5. Validação executada

| Verificação | Comando | Resultado |
|---|---|---|
| Build | `dotnet build Financeiro.slnx --no-restore` | 14 projetos, 0 erros, 0 warnings |
| Formatação | `dotnet format Financeiro.slnx --no-restore --verify-no-changes` | exit 0 |
| PostgreSQL `/health` | `ASPNETCORE_ENVIRONMENT=Development dotnet bin/Debug/net10.0/Financeiro.Api.dll --urls http://localhost:5196` | 200 em 2s |
| SQL Server `/health` | idem + `Database__Provider=SqlServer` (porta 5195) | 200 em 2s |
| Conexão real — PostgreSQL | `GET /api/v1/auth/csrf` + `POST /api/v1/auth/login` (e-mail inexistente) via HTTPS | 401 (consulta executada, sem 500) |
| Conexão real — SQL Server | mesmo probe com `Database__Provider=SqlServer` | 401 (consulta executada, sem 500) |
| Schema SQL Server | `sqlcmd ... -d financeiro -Q "SELECT COUNT(*) FROM sys.tables"` | 50 tabelas / 4 migrations |
| User secrets | `dotnet user-secrets list` | `No secrets configured for this application.` |

Observação: `/health` usa `AddHealthChecks()` **sem** verificação de banco, então sozinho não prova conectividade; o probe
de login (que consulta `AspNetUsers` via Identity) foi usado como evidência real em cada provider.

## 6. Riscos e pendências

- **Segredo em arquivo versionado:** `appsettings.Development.json` está no Git, e agora contém as senhas locais dos
  containers. Mitigação sem código: sobrescrever por `ConnectionStrings__PostgreSql` / `ConnectionStrings__SqlServer` /
  `ConnectionStrings__Financeiro`, ou trocar por placeholders se o repositório for compartilhado.
- **Senhas duplicadas:** manter `infra/.env` e `appsettings.Development.json` em sincronia ao trocar senhas locais.
- **Ambiente Production:** `dotnet run --no-launch-profile` inicia em Production; nesse caso a conexão precisa vir de
  variável de ambiente (`ConnectionStrings__<Provider>`), pois `appsettings.Development.json` não é carregado.
- **Health check sem banco:** avaliar incluir `AddDbContextCheck`/`AddNpgSql`/`AddSqlServer` para o `/health` refletir a
  conectividade real.
- **Arquivos modificados (não commitados):** `Projetos/README.md`, `Financeiro.Api.csproj`,
  `appsettings.Development.json`, `DatabaseConfiguration.cs`; `docs/` está como diretório não rastreado.
