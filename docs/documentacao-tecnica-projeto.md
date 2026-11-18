# Documentação técnica do projeto Financeiro

**Data:** 2026-11-18  
**Ambiente de referência:** Linux, .NET SDK 10.0.403, Node 26.10.0, npm 12.2.0  
**Escopo:** arquitetura, execução local, segurança, persistência, testes e operação técnica

## 1. Visão geral

O Financeiro é um SaaS de organização financeira com suporte a múltiplos espaços financeiros por usuário. A aplicação separa:

- **API .NET:** regras de negócio, autenticação, autorização, importação, auditoria e persistência.
- **Frontend React/Vite:** interface web, formulários, navegação, filtros e consumo da API.
- **Banco relacional:** PostgreSQL ou SQL Server, um provider por ambiente.
- **Infraestrutura local:** Docker Compose para os bancos e scripts Bash para build, migração, testes e desenvolvimento.

O repositório mantém as implementações em `Projetos/`, os documentos de domínio em `Guia/`, as especificações de mudança em `openspec/` e esta documentação em `docs/`.

## 2. Estrutura do repositório

| Caminho | Responsabilidade |
|---|---|
| `Projetos/src/Financeiro.Api` | API HTTP, controllers, middleware e configuração da aplicação |
| `Projetos/src/Financeiro.Application` | contratos, validações e regras de aplicação |
| `Projetos/src/Financeiro.Domain` | entidades e conceitos do domínio |
| `Projetos/src/Financeiro.Infrastructure` | Entity Framework Core, serviços, leitores de planilha e persistência |
| `Projetos/src/Financeiro.Migrations.PostgreSql` | migrations e snapshot do PostgreSQL |
| `Projetos/src/Financeiro.Migrations.SqlServer` | migrations e snapshot do SQL Server |
| `Projetos/src/Financeiro.Migrator` | aplicação explícita de migrations |
| `Projetos/frontend` | cliente React 19, TypeScript, Vite e Vitest |
| `Projetos/tests` | testes unitários, de arquitetura, integração e API |
| `Projetos/infra` | Docker Compose, imagens fixadas e configuração local |
| `Projetos/scripts` | scripts de build, CI, banco, migração e testes |

## 3. Pré-requisitos

- .NET SDK `10.0.403`, fixado em `Projetos/global.json`.
- Node `26.10.0` e npm `12.2.0`.
- Docker Engine e Docker Compose.
- Acesso aos feeds NuGet e npm para restauração das dependências.
- Certificado HTTPS de desenvolvimento confiável para executar a API localmente.

## 4. Execução local

### 4.1 Configurar o banco

Crie o arquivo local de ambiente a partir do exemplo e defina senhas que não sejam versionadas:

```bash
cp Projetos/infra/.env.example Projetos/infra/.env
```

Inicie um provider por vez:

```bash
bash Projetos/scripts/dev/database.sh postgres
# ou
bash Projetos/scripts/dev/database.sh sqlserver
```

Portas padrão dos containers:

| Provider | Host local |
|---|---:|
| PostgreSQL | `localhost:55432` |
| SQL Server | `localhost:51433` |

Os volumes são persistentes. Não remova volumes para alternar provider em ambientes que contenham dados relevantes.

### 4.2 Aplicar migrations

Use uma connection string apropriada ao provider:

```bash
cd Projetos
export FINANCEIRO_MIGRATION_CONNECTION='<connection-string-local>'
bash scripts/database/migrate.sh PostgreSql Development
```

Para SQL Server, substitua `PostgreSql` por `SqlServer`. O migrator aplica migrations pendentes e não recria nem apaga o banco.

### 4.3 Iniciar a API

O projeto exige HTTPS porque a autenticação usa cookies `__Host-` seguros. No ambiente utilizado para desenvolvimento deste projeto:

```bash
cd Projetos
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=https://localhost:5274 \
dotnet run --project src/Financeiro.Api --no-launch-profile
```

Verificações:

- API: `https://localhost:5274`
- Saúde: `https://localhost:5274/health`
- OpenAPI: `https://localhost:5274/openapi/v1.json`
- Swagger: `https://localhost:5274/swagger/index.html`

As portas do `launchSettings.json` podem variar; quando necessário, use `ASPNETCORE_URLS` explicitamente.

### 4.4 Iniciar o frontend

```bash
cd Projetos/frontend
FINANCEIRO_API_URL=https://localhost:5274 npm run dev -- --host localhost
```

O frontend fica disponível em `http://localhost:5173` e encaminha `/api` para a API configurada por `FINANCEIRO_API_URL`.

## 5. Arquitetura da aplicação

### Backend

O backend segue uma separação por camadas:

1. **API:** recebe HTTP, aplica autenticação/autorização e encaminha para serviços.
2. **Application:** define contratos, validações e regras independentes de infraestrutura.
3. **Domain:** contém entidades e estados do domínio financeiro.
4. **Infrastructure:** implementa serviços, consultas e persistência com EF Core.

Os módulos principais são dashboard, cadastros, obrigações e títulos, recorrências, faturas, pagamentos, acordos, viagens, posições de conta, auditoria e importação de planilhas.

### Frontend

O cliente usa React, TypeScript, React Router, TanStack Query, React Hook Form e Zod. As páginas ficam organizadas por funcionalidade em `Projetos/frontend/src/features/`, com APIs e contratos compartilhados em `src/shared/`.

O estado de dados é escopado pelo tenant e pelo espaço financeiro ativo. Ao trocar de espaço ou sair da conta, as consultas financeiras devem ser invalidadas para evitar exibição de dados de outro contexto.

## 6. Contexto, autenticação e autorização

O fluxo de primeira parte é:

1. Obter CSRF em `GET /api/v1/auth/csrf`.
2. Enviar `X-CSRF-TOKEN` em mutações.
3. Fazer login ou cadastro.
4. Criar ou selecionar tenant/espaço.
5. Consultar os módulos no espaço ativo.

Características de segurança:

- cookie de sessão `__Host-financeiro-auth`;
- cookie CSRF `__Host-financeiro-csrf`;
- cookies `HttpOnly`, `Secure` e `SameSite=Strict`;
- expiração ociosa de 30 minutos e limite absoluto de 8 horas;
- bloqueio após cinco falhas de senha por 15 minutos;
- MFA suportado por configuração, embora a tela de matrícula ainda não esteja disponível;
- permissões avaliadas por espaço, não por role técnica global;
- isolamento de tenant e espaço em consultas e gravações.

As mutações protegidas exigem sessão e token CSRF. CORS com credenciais para origens não confiáveis não deve ser habilitado.

## 7. Persistência e providers

O modelo lógico é mantido em dois assemblies de migration independentes:

- `Financeiro.Migrations.PostgreSql`;
- `Financeiro.Migrations.SqlServer`.

O provider ativo é escolhido por `Database:Provider`. A connection string específica pode ser substituída por variáveis de ambiente, preferencialmente sem editar arquivos versionados:

```bash
ConnectionStrings__PostgreSql='<connection-string>'
ConnectionStrings__SqlServer='<connection-string>'
ConnectionStrings__Financeiro='<connection-string-prioritária>'
```

O banco deve ser migrado antes de iniciar fluxos que dependam de Identity, contexto financeiro ou importação.

## 8. Importação de planilhas

O endpoint `POST /api/v1/importacoes` aceita somente `.xlsx`, com os limites:

- 10 MB por arquivo;
- 20 abas;
- 5.000 linhas por aba;
- 200 colunas;
- 500.000 células;
- 200 issues reportadas.

O processo é controlado:

1. upload;
2. análise das abas, inclusive ocultas;
3. armazenamento temporário dos registros, valores, referências e comentários;
4. revisão e decisão por registro;
5. confirmação dos registros aprovados.

O parser lê valores cacheados de fórmulas, mas não recalcula fórmulas. O arquivo binário original não é retido após a análise. A confirmação cria ou reaproveita obrigações, títulos, itens de viagem, recorrências, posições e evidências históricas conforme os campos reconhecidos.

Registros de previsão, cenário, backup e antecipação permanecem separados dos valores efetivos. Marcadores de pagamento geram evidência histórica sem criar automaticamente uma movimentação bancária com data e meio desconhecidos.

## 9. Observabilidade e auditoria

Respostas de erro usam `ProblemDetails` e incluem `traceId`/`correlationId`. A API propaga `X-Correlation-Id` para facilitar a correlação de logs.

Operações sensíveis são auditadas no espaço ativo, incluindo onboarding, administração de acesso, decisões e confirmação de importação, pagamentos, acordos, liquidações, cancelamentos e operações reversíveis.

Logs não devem conter senhas, cookies, cargas completas, parâmetros SQL, mensagens sensíveis de exceção ou identificadores bancários literais.

## 10. Testes e validação

Build reproduzível:

```bash
bash Projetos/scripts/build/build.sh
```

Checks do backend:

```bash
cd Projetos
dotnet format Financeiro.slnx --no-restore --verify-no-changes
dotnet build Financeiro.slnx --no-restore --configuration Release
dotnet test Financeiro.slnx --no-build --no-restore --configuration Release --filter 'Category!=Database'
```

Checks do frontend:

```bash
cd Projetos/frontend
npm run build
npm test
```

Verificação equivalente ao CI:

```bash
bash Projetos/scripts/ci/verify-clean-checkout.sh
```

Testes dependentes de banco exigem `FINANCEIRO_TEST_PROVIDER` e `FINANCEIRO_TEST_CONNECTION`, além do container saudável.

## 11. Operação e cuidados

- Faça backup antes de aplicar migrations em ambientes persistentes.
- Não use credenciais locais em produção.
- Preserve as chaves de Data Protection entre reinícios de instâncias reais.
- Não apague ou reescreva migrations já aplicadas; faça correções para frente.
- Não altere `bin/`, `obj/`, `dist/`, `node_modules/` ou caches gerados como parte de uma mudança funcional.
- Mantenha PostgreSQL e SQL Server alinhados em modelo, migrations e testes.

## 12. Limitações conhecidas

- Não há sincronização bancária automática.
- Não há livro completo de receitas ou contabilidade de transferências.
- Não há migração automática entre PostgreSQL e SQL Server.
- A importação não substitui revisão manual e não transforma todos os campos arbitrários da planilha em entidades financeiras.
- A interface de matrícula e gestão de MFA ainda não está disponível.
