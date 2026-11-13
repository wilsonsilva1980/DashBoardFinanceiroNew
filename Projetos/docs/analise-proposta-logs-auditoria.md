# Análise e proposta de solução: logs e auditoria consultável

**Projeto:** Financeiro SaaS  
**Data da análise:** 13/11/2026  
**Escopo:** backend ASP.NET Core/EF Core, PostgreSQL e SQL Server, frontend React/TypeScript e testes

## 1. Sumário executivo

O projeto já possui a base correta para auditoria de negócio: a entidade `AuditRecord`, a tabela `auditoria`, isolamento por `tenant_id`/`espaco_id`, `correlation_id`, registro do usuário e eventos críticos persistidos na mesma transação das alterações financeiras.

O gap principal não é criar uma segunda trilha de auditoria, mas completar o processo de ponta a ponta:

1. padronizar o catálogo e o formato dos eventos;
2. centralizar a criação dos registros para reduzir duplicação nos serviços;
3. disponibilizar consulta paginada e filtrada protegida por `auditoria.visualizar`;
4. criar a tela de consulta no frontend;
5. reforçar índices, retenção, redaction de dados sensíveis e testes nos dois providers;
6. manter separados os logs operacionais de aplicação e a auditoria funcional.

Recomendação: evoluir a tabela `auditoria` existente para a trilha de auditoria funcional e usar o `ILogger` estruturado para diagnóstico técnico. Criar uma tabela de logs operacionais no banco somente se houver requisito explícito de consulta histórica centralizada; por padrão, esses logs devem seguir para o sink de observabilidade da implantação.

## 2. Diagnóstico do estado atual

### 2.1 Arquitetura encontrada

- Monólito modular com Clean Architecture.
- API ASP.NET Core com controllers, políticas de autorização e OpenAPI.
- EF Core com um `FinanceiroDbContext` e assemblies de migration independentes para PostgreSQL e SQL Server.
- Frontend React 19/TypeScript/Vite, React Router e TanStack Query.
- Escopo financeiro resolvido no servidor a partir da sessão: `tenant` + espaço financeiro.
- Cookies de autenticação `__Host-`, CSRF em mutações e correlação por `X-Correlation-Id`.
- Permissões por espaço, incluindo `auditoria.visualizar` no catálogo de permissões.

### 2.2 Auditoria já implementada

`AuditRecord` possui atualmente:

| Campo | Finalidade |
|---|---|
| `Id` | identificador do evento |
| `TenantId`, `SpaceId` | isolamento da informação |
| `UserId` | usuário que originou a ação; pode ser nulo em eventos de sistema |
| `Action` | código do evento, por exemplo `pagamento.confirmado` |
| `Entity`, `EntityId` | objeto de negócio afetado |
| `Instant` | instante UTC do evento |
| `CorrelationId` | correlação entre requisição e trilha técnica |
| `ChangesJson` | dados mínimos da alteração, em JSON |
| campos de `Entity` | versão, timestamps e `CreatedBy` |

A tabela física é `auditoria`. Há chave composta alternativa com tenant/espaço, FK composta para `espacos`, FK restrita para usuário e índice inicial por `(tenant_id, espaco_id, instant)`.

Eventos já observados no código incluem:

- `onboarding.confirmed`;
- `space.member.deactivated` e `tenant.member.deactivated`;
- `pagamento.confirmado` e `pagamento.revertido`;
- `liquidacao-historica.registrada` e `liquidacao-historica.revogada`;
- `acordo.confirmado`;
- `titulo.cancelado`, `titulo.excluido` e `obrigacao.excluida`.

Os serviços já adicionam auditoria antes de `SaveChangesAsync`, inclusive dentro de transações nos fluxos financeiros críticos. Isso preserva a propriedade desejada: uma operação confirmada não pode ser persistida sem seu evento de auditoria correspondente.

### 2.3 Lacunas e riscos

- Não existe serviço único para validar nomes, tamanho e conteúdo de eventos; cada serviço monta `AuditRecord` diretamente.
- `Action` e `Entity` são strings livres, sujeitos a variações de nomenclatura.
- `ChangesJson` tem limite de 8.000 caracteres e não há validação explícita de JSON/redaction.
- Há índice temporal básico, mas não há índices específicos para consulta por ação, entidade ou usuário.
- Não há endpoint de leitura nem tela de auditoria, apesar de existir a permissão `auditoria.visualizar`.
- A lista de permissões não mostra, no frontend atual, controle visível de `auditoria.visualizar`.
- A auditoria de negócio não deve ser usada como substituta de logs de infraestrutura: falhas de banco, duração de requisição, status HTTP e exceções precisam continuar em logs estruturados técnicos.
- A tabela atual não contém explicitamente método HTTP, rota, resultado, IP ou User-Agent. Esses dados podem ser úteis para segurança, mas devem ser adicionados somente com política de retenção e privacidade definida.

## 3. Objetivos e não objetivos

### Objetivos

- registrar operações relevantes de negócio de forma consistente e imutável na prática;
- consultar auditoria somente dentro do espaço ativo e conforme permissão;
- filtrar por período, ação, entidade, usuário, `EntityId` e `CorrelationId`;
- permitir paginação estável, ordenada do evento mais recente para o mais antigo;
- visualizar detalhes JSON sem expor segredos, tokens, senhas ou identificadores bancários completos;
- manter compatibilidade com PostgreSQL e SQL Server;
- tornar a funcionalidade testável por unidade, integração e E2E.

### Fora do escopo inicial

- edição ou exclusão de eventos de auditoria pela UI;
- auditoria de cada leitura simples de tela;
- armazenamento de stack trace ou payload HTTP completo na tabela de auditoria;
- exportação sem limite ou sem autorização;
- central de observabilidade substituindo um sink externo de logs.

## 4. Solução proposta

### 4.1 Separação conceitual

```text
[Comando de negócio]
        |
        +--> alteração financeira
        |
        +--> AuditRecord --> tabela auditoria
        |                    (transacional, consultável, por espaço)
        |
        +--> ILogger ------> sink técnico
                             (diagnóstico, desempenho, exceções)
```

**Auditoria funcional** responde “quem fez o quê, em qual registro e quando?”. Deve ser persistida no banco na mesma transação da operação.

**Log operacional** responde “o que ocorreu com a aplicação e sua infraestrutura?”. Deve ser estruturado e encaminhado para console, collector ou plataforma de observabilidade. Não deve conter dados financeiros ou de autenticação em claro.

### 4.2 Serviço central de auditoria

Criar no núcleo de aplicação uma abstração semelhante a:

```csharp
public interface IAuditWriter
{
    void Record(
        string action,
        string entity,
        Guid entityId,
        object? changes = null,
        Guid? userId = null);
}
```

A implementação deve:

- obter `TenantId`/`SpaceId` de `FinancialScope`, nunca do corpo da requisição;
- obter usuário e correlação de `IRequestActor`;
- gerar `Guid` e instante UTC no servidor;
- serializar JSON com opções determinísticas;
- remover ou mascarar campos sensíveis por uma política única;
- limitar o tamanho final e rejeitar eventos inválidos;
- inserir no mesmo `DbContext` da operação, deixando o commit para o caso de uso;
- aceitar usuário nulo apenas para eventos de sistema explicitamente permitidos.

O serviço não deve chamar `SaveChanges` sozinho. Assim, o evento participa da mesma transação da alteração de negócio.

### 4.3 Catálogo de eventos

Substituir strings espalhadas por constantes ou um catálogo tipado, mantendo os códigos atuais para compatibilidade. Exemplos:

| Código | Entidade | Quando registrar |
|---|---|---|
| `auth.login.succeeded` | `UserSession` | login concluído |
| `auth.login.failed` | `UserSession` | somente se houver requisito de segurança; sem senha/payload |
| `auth.logout` | `UserSession` | sessão encerrada |
| `context.selected` | `FinancialSpace` | troca de espaço autorizada |
| `payment.confirmed` | `Payment` | pagamento confirmado |
| `payment.reversed` | `Payment` | pagamento estornado |
| `historical_settlement.registered` | `HistoricalSettlement` | baixa histórica registrada |
| `historical_settlement.revoked` | `HistoricalSettlement` | baixa histórica revogada |
| `agreement.confirmed` | `SettlementAgreement` | acordo confirmado |
| `title.cancelled` | `PayableTitle` | título cancelado |
| `title.deleted` | `PayableTitle` | exclusão permitida |
| `access.member.deactivated` | `SpaceMember`/`TenantMember` | membro desativado |
| `audit.exported` | `AuditRecord` | exportação autorizada, se implementada |

A migração dos códigos atuais pode ser gradual. O contrato público deve escolher um idioma e uma convenção; recomendo códigos estáveis em inglês técnico, mantendo rótulos em pt-BR apenas na UI.

### 4.4 Estrutura de tabelas

#### Tabela existente: `auditoria`

Manter a estrutura atual como base. Recomenda-se evoluir com os campos abaixo somente se forem necessários para segurança e rastreabilidade:

| Campo proposto | Tipo lógico | Observação |
|---|---|---|
| `event_type` | varchar(100) | código estável; substitui gradualmente `action` |
| `outcome` | varchar(30) | `Succeeded`, `Denied`, `Failed`; opcional para eventos somente de negócio |
| `request_method` | varchar(10) | GET/POST/etc.; não deve ser usado como autorização |
| `request_path` | varchar(300) | rota normalizada, sem query string sensível |
| `ip_hash` | varchar(128) | hash com salt rotacionável, se necessário; evitar IP cru |
| `metadata_json` | texto/JSON | metadados técnicos aprovados; separar de mudanças de negócio |

Não adicionar todos esses campos automaticamente. Para o primeiro incremento, basta manter o modelo atual, criar `IAuditWriter`, padronizar `Action`/`Entity` e adicionar os endpoints de consulta. A evolução de schema pode ser feita quando houver requisito de segurança para origem da chamada.

#### Tabela opcional: `logs_aplicacao`

Só criar se o produto exigir consulta de logs técnicos pelo próprio sistema. Nesse caso:

| Campo | Tipo lógico | Regra |
|---|---|---|
| `id` | UUID | PK |
| `instant` | DateTimeOffset | UTC |
| `level` | varchar(20) | Trace/Debug/Information/Warning/Error |
| `category` | varchar(200) | categoria do `ILogger` |
| `event_name` | varchar(100) | código estável |
| `message_template` | varchar(500) | sem interpolação de segredo |
| `properties_json` | texto/JSON | propriedades redigidas |
| `exception_type` | varchar(300), nulo | tipo, sem stack trace por padrão |
| `correlation_id` | varchar(200) | correlação |
| `tenant_id`, `espaco_id` | UUID, nulos | nulos para eventos fora do contexto |
| `user_id` | UUID, nulo | usuário quando conhecido |

Essa tabela deve ser particionável/retentiva e escrita de forma assíncrona, com proteção contra falha de logging afetar a operação financeira. Para o cenário atual, ela é uma alternativa futura e não a recomendação inicial.

### 4.5 Índices e retenção

Para `auditoria`, criar migrations em ambos os providers com índices equivalentes:

- `(tenant_id, espaco_id, instant DESC)` para a listagem principal;
- `(tenant_id, espaco_id, action, instant DESC)` para filtro por ação;
- `(tenant_id, espaco_id, user_id, instant DESC)` para filtro por usuário;
- `(tenant_id, espaco_id, entity, entity_id, instant DESC)` para histórico do registro;
- `(tenant_id, espaco_id, correlation_id)` para investigação de requisição.

Validar os nomes e sintaxe específicos no snapshot de cada provider; não copiar SQL de PostgreSQL para SQL Server.

Retenção recomendada como configuração do ambiente, não como exclusão pela tela. Exemplo inicial: manter auditoria por 24 meses, com job administrativo e registro de qualquer expurgo. Em produção, a decisão deve considerar requisitos legais, volume e backup.

## 5. API proposta

### 5.1 Endpoint de consulta

`GET /api/v1/auditoria`

Parâmetros:

| Parâmetro | Tipo | Regra |
|---|---|---|
| `from` | `YYYY-MM-DD` | inclusivo; padrão últimos 30 dias |
| `until` | `YYYY-MM-DD` | inclusivo; limite máximo configurável |
| `action` | string | filtro exato por evento |
| `entity` | string | filtro exato |
| `entityId` | UUID | registro específico |
| `userId` | UUID | usuário que originou |
| `correlationId` | string | correlação exata |
| `page` | inteiro | começa em 1 |
| `pageSize` | inteiro | 20 padrão, máximo 100 |

Resposta:

```json
{
  "items": [
    {
      "id": "uuid",
      "instant": "2026-11-13T14:30:00Z",
      "action": "pagamento.confirmado",
      "actionLabel": "Pagamento confirmado",
      "entity": "Payment",
      "entityId": "uuid",
      "userId": "uuid",
      "userDisplayName": "Nome do usuário",
      "correlationId": "abc123",
      "changes": { "amount": "100.00", "currency": "BRL" }
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 20
}
```

Regras obrigatórias do endpoint:

- `[Authorize(Policy = "auditoria.visualizar")]`;
- exigir contexto financeiro ativo;
- usar `AsNoTracking` e projeção, sem carregar entidades financeiras relacionadas;
- aplicar o filtro global de escopo e validar explicitamente o espaço;
- ordenar por `instant DESC, id DESC` para paginação determinística;
- não aceitar `tenantId` ou `spaceId` como fonte de autorização;
- limitar período e tamanho da página;
- converter `ChangesJson` para objeto apenas após validar JSON; se inválido, retornar conteúdo seguro ou `{}`;
- não retornar IP, User-Agent, tokens, cookies ou payloads completos.

### 5.2 Endpoint de catálogo

`GET /api/v1/auditoria/eventos`

Retorna os eventos permitidos para popular o filtro da UI, com código e rótulo pt-BR. O catálogo deve ser estático ou derivado de constantes, não de valores arbitrários gravados no banco.

### 5.3 Exportação futura

Se for necessária, usar `GET /api/v1/auditoria/export` com:

- mesma permissão `auditoria.visualizar` e, opcionalmente, `relatorios.exportar`;
- limite de período e volume;
- auditoria do próprio evento `audit.exported`;
- geração assíncrona para grandes volumes;
- CSV sem JSON sensível bruto ou com política de redaction explícita.

## 6. Tela de consulta no frontend

Criar a feature `frontend/src/features/audit/` e rota `/auditoria`.

### 6.1 Navegação

Adicionar “Auditoria” no menu somente quando a permissão `auditoria.visualizar` estiver presente. A proteção visual é apenas conveniência; a API continua sendo a autoridade.

### 6.2 Layout recomendado

```text
+---------------------------------------------------------------+
| Auditoria do espaço                                          |
| [Período] [Ação] [Entidade] [Usuário] [Buscar] [Limpar]      |
+---------------------------------------------------------------+
| Data/hora | Ação | Entidade | Usuário | Correlação | Detalhes|
| ...                                                          |
+---------------------------------------------------------------+
| Página 1 de N                         [Anterior] [Próxima]    |
+---------------------------------------------------------------+
```

Ao selecionar uma linha, abrir `Dialog` existente com:

- data/hora em timezone da interface;
- ação com rótulo amigável e código técnico;
- entidade e identificador;
- usuário;
- correlação com botão de copiar;
- JSON formatado, com campos já redigidos pelo backend.

Usar TanStack Query, `get` do cliente existente e `useScopeKey()` para impedir cache entre espaços. A tela deve tratar loading, erro, vazio e paginação, seguindo `QueryStatus` e os padrões já usados pelas features.

### 6.3 Acessibilidade e segurança

- filtros com labels explícitos e suporte a teclado;
- não exibir JSON em HTML sem escaping;
- não permitir edição, remoção ou reprocessamento;
- esconder a rota da navegação quando a permissão não existir, mas aceitar 403 do servidor;
- invalidar a query ao trocar de espaço ou sair.

## 7. Plano de implementação

### Fase 1 — padronização e serviço

1. Criar `IAuditWriter` e implementação em `Financeiro.Infrastructure`.
2. Criar catálogo de eventos e rótulos.
3. Migrar os serviços que atualmente fazem `new AuditRecord` diretamente.
4. Centralizar serialização, tamanho máximo e redaction.
5. Adicionar logs `ILogger` estruturados nos limites de requisição e exceção, sem payload sensível.

### Fase 2 — consulta backend

1. Criar contratos de aplicação para filtros e página.
2. Criar `AuditQueryService`/caso de uso somente leitura.
3. Criar `AuditController` e endpoint de catálogo.
4. Adicionar política de autorização e checagem de escopo.
5. Criar/atualizar índices e migrations PostgreSQL/SQL Server.

### Fase 3 — frontend

1. Criar contratos TypeScript.
2. Criar `AuditPage` e modal de detalhes.
3. Integrar permissão no menu e rota.
4. Adicionar filtros, paginação, estados de erro e cache por espaço.

### Fase 4 — qualidade e operação

1. Testar isolamento entre espaços e tenants.
2. Testar autorização do Leitor e de perfis sem `auditoria.visualizar`.
3. Testar ordenação, filtros, limites e JSON inválido.
4. Testar migrations nos dois providers.
5. Definir retenção, métricas e alertas com a operação.

## 8. Estratégia de testes

### Backend unitário

- catálogo sem códigos duplicados;
- redaction de senha, token, cookie, número de conta e comentários sensíveis;
- truncamento/rejeição de `ChangesJson` acima do limite;
- serialização determinística;
- ausência de escopo impede gravação;
- usuário nulo permitido somente em evento de sistema.

### Integração API

- usuário com `auditoria.visualizar` consulta apenas o espaço ativo;
- usuário sem a permissão recebe 403;
- ID de outro espaço retorna vazio/404 conforme o contrato, nunca dados;
- filtros por ação, entidade, entidade/ID, usuário e correlação;
- paginação máxima e período máximo são aplicados;
- evento de negócio e auditoria são confirmados ou revertidos juntos;
- troca de espaço não reaproveita cache ou dados do espaço anterior.

### Migrations/provider

- criação dos índices nos dois snapshots;
- aplicação repetida sem duplicação;
- constraints e FKs continuam restritivas;
- consulta de data e ordenação equivalentes em PostgreSQL e SQL Server.

### Frontend/E2E

- menu e rota aparecem somente com permissão;
- filtros e paginação atualizam a query;
- modal mostra detalhes com JSON escapado;
- loading, erro e lista vazia são apresentados;
- troca de espaço limpa a lista anterior.

## 9. Critérios de aceite

- Todo evento de auditoria novo passa pelo serviço central.
- Eventos críticos permanecem na mesma transação da operação de negócio.
- A consulta nunca atravessa `tenant` ou espaço financeiro.
- A API e a tela funcionam nos perfis autorizados e retornam 403 nos demais.
- A tela permite filtrar por período, ação, entidade, usuário e correlação, com paginação.
- Nenhum segredo ou dado bancário completo aparece na auditoria ou na UI.
- Migrations, build e testes passam para PostgreSQL e SQL Server.
- Logs técnicos continuam disponíveis via `ILogger`/sink sem depender da tabela de auditoria.
- Retenção e exportação, se habilitadas, são configuráveis e auditáveis.

## 10. Decisões recomendadas

1. **Não criar imediatamente `logs_aplicacao`:** a tabela de auditoria existente atende ao requisito funcional e o `ILogger` atende diagnóstico técnico.
2. **Centralizar a escrita:** evita divergências entre serviços e facilita redaction e evolução.
3. **Manter auditoria append-only na aplicação:** sem endpoints de edição/exclusão; expurgo somente por job administrativo controlado.
4. **Autorizar por `auditoria.visualizar`:** a permissão já existe no modelo e deve ser conectada à API e à navegação.
5. **Começar com o schema atual:** acrescentar colunas somente quando requisitos de segurança/observabilidade justificarem nova migration.
6. **Tratar a auditoria como dado de segurança:** backup, retenção, acesso e exportação devem fazer parte da operação do produto.

## 11. Referências analisadas

- `Projetos/README.md`
- `Projetos/src/Financeiro.Domain/Entities/AuditRecord.cs`
- `Projetos/src/Financeiro.Infrastructure/Persistence/FinanceiroDbContext.cs`
- `Projetos/src/Financeiro.Infrastructure/Persistence/Configurations/Common/AuditRecordConfiguration.cs`
- `Projetos/src/Financeiro.Infrastructure/Persistence/FinancialModelConventions.cs`
- `Projetos/src/Financeiro.Infrastructure/Access/PermissionEvaluator.cs`
- `Projetos/src/Financeiro.Application/Access/Onboarding.cs`
- serviços de acesso, obrigações, pagamentos, acordos e liquidações históricas
- migrations iniciais de PostgreSQL e SQL Server
- `Projetos/frontend/src/App.tsx`, cliente de API e features existentes
- `Guia/Arquitetura_Sistema_Financeiro_NovaPlanilhaDividas2_v4_Consolidada_SaaS.md`
- `Projetos/docs/verification-persistence-security.md`

