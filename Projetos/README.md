# Financeiro

Implementação do SaaS financeiro. Contratos de negócio e arquitetura: [guia consolidado](../Guia/Arquitetura_Sistema_Financeiro_NovaPlanilhaDividas2_v4_Consolidada_SaaS.md).

## Pré-requisitos

- Linux com Bash, .NET SDK 10.0.403 no PATH, Node 26.10.0 e npm 12.2.0.
- Acesso a NuGet e npm para restaurar as dependências travadas.
- Docker Engine com Compose para os bancos locais.

## Build reproduzível

Da raiz do repositório: `bash Projetos/scripts/build/build.sh`.
O script também pode ser chamado por caminho absoluto a partir de qualquer diretório.

## Desenvolvimento

Copie `Projetos/infra/.env.example` para `Projetos/infra/.env` e defina senhas locais fortes (SQL Server exige pelo menos 8 caracteres e complexidade). As imagens são fixadas em `infra/images.env`.

Da raiz, inicie um provider por vez: `bash Projetos/scripts/dev/database.sh postgres` ou `bash Projetos/scripts/dev/database.sh sqlserver`.
Os serviços escutam apenas em localhost, nas portas 55432 e 51433. Compose mantém volumes persistentes; não apague volumes para trocar provider.

Em `Projetos/`, configure variáveis de ambiente e execute a API:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export Database__Provider=PostgreSql
export ConnectionStrings__Financeiro='Host=localhost;Port=55432;Database=financeiro;Username=financeiro;Password=SUA_SENHA_LOCAL'
dotnet run --project src/Financeiro.Api --no-launch-profile --urls http://localhost:5000
```

Para SQL Server, use `Database__Provider=SqlServer` e `ConnectionStrings__Financeiro='Server=localhost,51433;Database=master;User Id=sa;Password=SUA_SENHA_LOCAL;Encrypt=True;TrustServerCertificate=True'`.
`master` é usado somente para verificar conectividade nesta etapa; a aplicação usará banco próprio quando o migrator estiver implementado. `sa` e confiança no certificado local são exclusivos do desenvolvimento; produção exige usuário restrito e certificado válido.
Em outro terminal, entre em `Projetos/frontend/` e execute `npm run dev`.
O frontend encaminha `/api` para a API local. Saúde da API: `/health`.

O backend já oferece autenticação, onboarding, seleção de contexto e proteção de acessos. As telas e operações dos módulos financeiros permanecem nas tarefas seguintes.

## Testes da base de infraestrutura

Em `Projetos/`, após o restore, configure `FINANCEIRO_TEST_PROVIDER` (`PostgreSql` ou `SqlServer`) e `FINANCEIRO_TEST_CONNECTION` com a conexão local correspondente. Execute `bash scripts/test/provider.sh`. O teste de conexão exige o container saudável; não é ignorado silenciosamente se a configuração faltar.

A matriz inclui contratos do modelo, migrations incrementais, isolamento, onboarding concorrente, autorização, CSRF e revogação de sessões. Testes adicionais dos módulos financeiros serão acrescentados nas próximas etapas.

## CI e validação de checkout limpo

O workflow `.github/workflows/financeiro-ci.yml` executa formatação/build/testes .NET, build/testes TypeScript e a matriz de migrations/integração PostgreSQL e SQL Server. As ações GitHub são fixadas por SHA.

Com .NET e Node/npm no PATH, execute da raiz:

```bash
bash Projetos/scripts/ci/verify-clean-checkout.sh
```

O script cria um snapshot Git temporário apenas de `Projetos/` e do workflow, faz um clone local sem arquivos de build/cache/segredos e executa os mesmos pontos de entrada do CI. Não altera o histórico Git do projeto e não publica conteúdo. Os caches globais de pacotes podem ser reutilizados, mas restore travado e builds começam sem `bin/`, `obj/` ou `node_modules/` no clone. Os containers usam um projeto Compose único e portas aleatórias; o script remove somente seus próprios containers/volumes descartáveis ao terminar.

### Migrations

Há um contexto lógico e dois assemblies com snapshots independentes. As factories têm provider fixo; a conexão entra por `FINANCEIRO_MIGRATION_CONNECTION`.

Para aplicar no banco de destino, informe provider e ambiente explicitamente:

```bash
export FINANCEIRO_MIGRATION_CONNECTION='SUA_CONEXAO_DO_BANCO_FINANCEIRO'
bash Projetos/scripts/database/migrate.sh PostgreSql Development
```

O migrator aplica migrations pendentes e não recria nem apaga o banco. Use banco próprio para a aplicação; `master` serve apenas à verificação de conexão do SQL Server. Para alterar o modelo, gere uma migration em cada assembly a partir de `Projetos/`, com `dotnet tool restore` e `dotnet ef migrations add Nome --project src/Financeiro.Migrations.PostgreSql` (depois `Financeiro.Migrations.SqlServer`). Confira os scripts e execute a matriz antes de liberar. Migrations já aplicadas devem ser evoluídas com correções para frente.

## Autenticação e contexto

A API usa cookies `__Host-` com `Secure`, `HttpOnly` e `SameSite=Strict`. Use HTTPS no ambiente real e preserve as chaves de Data Protection entre reinícios e instâncias. Nunca habilite CORS com credenciais para origens não confiáveis. As credenciais de desenvolvimento pertencem somente ao banco local.

Fluxo do cliente de primeira parte:

1. `GET /api/v1/auth/csrf` devolve `{token}` e define o cookie HttpOnly de proteção.
2. Envie `X-CSRF-TOKEN` nas mutações. `POST /api/v1/auth/register` recebe `email`, `password`, `displayName`; `POST /api/v1/auth/login` recebe `email`, `password` e `twoFactorCode` quando MFA estiver habilitado.
3. Depois de login, obtenha outro token CSRF. `GET /api/v1/auth/me` retorna o usuário e o contexto selecionado.
4. `POST /api/v1/context/onboarding` recebe `tenantName`, `spaceName`, `currency`, `requestKey`. Repetir chave e conteúdo devolve o mesmo contexto; alterar conteúdo com a mesma chave retorna 409.
5. `GET /api/v1/context/spaces` lista somente associações ativas autorizadas. `POST /api/v1/context/select` recebe `tenantId`, `spaceId` e valida as associações no servidor.
6. `POST /api/v1/auth/logout` revoga a sessão no banco. Reutilizar o cookie copiado não recupera acesso. Limpe o cache de finanças no cliente ao sair ou trocar de contexto.

O login usa senha com comprimento mínimo de 10 caracteres e as regras padrão de complexidade do Identity; cinco falhas bloqueiam novas tentativas por 15 minutos. A sessão tem expiração ociosa de 30 minutos e limite absoluto de 8 horas. Alterar o security stamp ou desativar o usuário invalida o acesso. A seção `Identity` da configuração permite estender a política de senha/lockout; `Identity:RequireMfa=true` exige matrícula MFA, e contas que já têm MFA habilitado exigem um código de autenticador válido. A interface de matrícula MFA ainda não faz parte desta etapa.

Perfis iniciais: Administrador, Gestor financeiro, Operador e Leitor. As permissões são avaliadas por espaço e não são concedidas por roles técnicas globais. Os endpoints de desativação de membros em `/api/v1/access/` exigem versão atual e preservam o último administrador em transação com lock. Administração de tenant e administração de espaço são verificadas separadamente.

Em Development, o contrato está em `/openapi/v1.json` e o Swagger em `/swagger/index.html`; o documento descreve cookie e CSRF. Em Production, esses endpoints de desenvolvimento não são expostos.

## Observabilidade e auditoria

- As respostas de erro em `ProblemDetails` incluem `traceId`/`correlationId`; o servidor propaga/retorna `X-Correlation-Id` para correlação entre cliente e logs.
- Logs estruturados registram tipo de falha, status, correlação e caminho. Cargas, parâmetros SQL, cookies, senhas, mensagens sensíveis de exceção e identificadores bancários literais não devem ser registrados.
- Operações confirmadas e sensíveis registram auditoria no espaço ativo (ex.: onboarding, gestão de acesso, confirmações/reversões de pagamento, liquidações históricas e revogações, confirmação de acordos, cancelamentos/exclusões auditáveis). A correlação da requisição é preservada no registro de auditoria.

## Dashboard e reconciliação

- `GET /api/v1/dashboard/resumo?from=YYYY-MM-DD&until=YYYY-MM-DD&currency=BRL` retorna resumos por categoria, responsável, vencimento, evolução mensal e posições, distinguindo valores efetivos, previsão, em aberto, parciais, pagos e vencidos.
- Indicadores de reconciliação (ex.: créditos excedentes, divergências de totais de origem, inconsistências de viagem) são retornados em `reconciliationIndicators` e apresentados separadamente na UI, sem compor totais normais de contas a pagar.
- `GET /api/v1/dashboard/fluxo-caixa` mantém visão de saídas datadas; posições de conta continuam sendo observações, não movimentações.

## Backup e rollback

- Cada implantação usa um único provider de banco (PostgreSQL **ou** SQL Server), sem replicação cruzada.
- Antes de aplicar migrations, realize backup completo/consistente do banco selecionado e valide restore em ambiente não produtivo. Mantenha janela de manutenção e plano de comunicação.
- Rollback de aplicação deve retornar a um build compatível com o schema aplicado. Não remova nem reescreva schema financeiro já aplicado como forma de rollback; correções devem ser feitas por migrations para frente e operações corretivas auditadas.
- Não apague volumes/dados para "trocar de provider" em ambiente com dados relevantes.

## Exclusões conhecidas da primeira versão

- Sem importação/parsing de planilhas, sincronização bancária, faturamento SaaS, livro completo de receitas ou contabilidade de transferências.
- Sem seleção de provider por cliente e sem migração automática entre PostgreSQL e SQL Server.
- Totais/comentários/fórmulas históricos da planilha não são tratados como transações financeiras verificadas; fatos desconhecidos permanecem desconhecidos.
- Interface de matrícula/gestão MFA ainda não faz parte desta etapa (política/extensões existem no backend).
