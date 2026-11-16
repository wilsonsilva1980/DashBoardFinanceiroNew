# Design

## Context

Consulte `proposal.md` para a motivação e `specs/audit-trail/spec.md` para o contrato comportamental. O projeto já possui `AuditRecord`, tabela `auditoria`, correlação de requisições, filtro global por tenant/espaço, permissão `auditoria.visualizar` e gravações em serviços de pagamentos, liquidações, acordos, obrigações, onboarding e acessos.

O modelo é compartilhado por PostgreSQL e SQL Server, com migrations e snapshots independentes. O frontend usa React Router, TanStack Query, um cliente HTTP de primeira parte com CSRF e cache particionado por contexto. A auditoria atual é criada diretamente por vários serviços, sem endpoint de leitura ou tela.

## Goals / Non-Goals

**Goals:**

- completar a trilha funcional usando a tabela `auditoria` existente;
- centralizar criação, serialização e redaction sem retirar o commit do caso de uso;
- oferecer consulta somente leitura, paginada, escopada e indexada;
- conectar a permissão existente ao backend e à navegação React;
- manter comportamento equivalente nos dois providers;
- separar auditoria de negócio de logs operacionais `ILogger`.

**Non-Goals:**

- criar inicialmente uma tabela `logs_aplicacao` para logs técnicos;
- registrar cada leitura de tela ou cada requisição HTTP como auditoria funcional;
- permitir editar, excluir ou reprocessar eventos pela UI;
- adicionar exportação ou expurgo automático neste incremento;
- guardar payloads HTTP completos, stack traces, cookies, tokens ou IP cru na auditoria.

## Decisions

### 1. Reutilizar `auditoria` como fonte funcional

Manter `AuditRecord` e a tabela existente como fonte da verdade. O primeiro incremento preserva as colunas atuais e adiciona apenas os índices necessários à consulta. Campos adicionais como método HTTP, resultado ou hash de IP ficam condicionados a um requisito de segurança explícito.

**Alternativa considerada:** criar uma nova tabela de eventos. Foi rejeitada porque duplicaria a trilha já transacional, exigiria migração dos eventos existentes e aumentaria o risco de duas fontes divergentes.

### 2. Serviço central de escrita sem `SaveChanges`

Criar uma abstração de escrita de auditoria que recebe ação, entidade, identificador e detalhes; resolve ator, correlação e escopo a partir de serviços de request; normaliza o evento e adiciona a entidade ao `DbContext`. O serviço não chama `SaveChanges`, garantindo que o evento participe da mesma transação do caso de uso.

**Alternativa considerada:** interceptor global de `SaveChanges`. Foi rejeitada para o primeiro passo porque não conhece a semântica de confirmação, reversão ou cancelamento e poderia registrar alterações técnicas ou estados intermediários que não são eventos de negócio.

### 3. Catálogo estável de eventos

Introduzir constantes/catálogo para códigos e rótulos. Eventos atuais devem ser preservados ou mapeados em compatibilidade, evitando que a tela dependa de strings arbitrárias gravadas no banco. Rótulos pt-BR ficam na camada de apresentação; códigos permanecem estáveis para integração e testes.

**Alternativa considerada:** catálogo persistido e editável. Foi rejeitado porque permitiria alterar o significado histórico de um código e não é necessário para a primeira versão.

### 4. Consulta por projeção e escopo do servidor

Criar caso de uso de consulta que usa `AsNoTracking`, projeção para DTO, filtro global e validação explícita do `FinancialScope`. A ordenação será `Instant DESC, Id DESC`; `pageSize` terá limite máximo. O endpoint não aceitará tenant/espaço como fonte de autorização.

Índices previstos em `auditoria`:

- `(tenant_id, espaco_id, instant)`;
- `(tenant_id, espaco_id, action, instant)`;
- `(tenant_id, espaco_id, user_id, instant)`;
- `(tenant_id, espaco_id, entity, entity_id, instant)`;
- `(tenant_id, espaco_id, correlation_id)`.

Os nomes e expressões de índice devem ser gerados/validados separadamente nos snapshots PostgreSQL e SQL Server.

### 5. API e UI somente leitura

Expor `GET /api/v1/auditoria` e `GET /api/v1/auditoria/eventos`, ambos sob `auditoria.visualizar`. A tela `/auditoria` usa o cliente HTTP e o cache já existentes, com chave incluindo o espaço ativo. Detalhes são exibidos em `Dialog` e sempre escapados; nenhuma ação de alteração aparece na interface.

**Alternativa considerada:** embutir auditoria em cada tela de entidade. Foi rejeitada porque dificulta filtros globais, suporte e evolução; detalhes por entidade podem ser adicionados depois consumindo a mesma API.

### 6. Redaction antes da persistência

Aplicar uma política central de campos proibidos antes de serializar `ChangesJson`. Campos de senha, token, cookie, segredo, número de conta completo e conteúdo sensível devem ser omitidos ou mascarados. O limite de tamanho deve ser validado antes de inserir o registro.

**Alternativa considerada:** redaction somente no frontend. Foi rejeitada porque dados inseguros permaneceriam no banco e poderiam vazar por outro consumidor da API.

### 7. Logs operacionais fora da tabela de auditoria

Usar `ILogger` estruturado para status HTTP, exceções, duração, falhas de banco e correlação. Não persistir esses eventos em `auditoria`; se houver requisito futuro de consulta pelo produto, criar `logs_aplicacao` com retenção e escrita assíncrona como mudança separada.

## Risks / Trade-offs

- **[Volume da tabela]** A auditoria pode crescer continuamente → definir retenção operacional e índices antes de produção; expurgo deve ser job administrativo auditado em mudança futura.
- **[Custo de `COUNT(*)`]** Paginação com total pode ficar cara em grandes volumes → limitar intervalo/página e avaliar cursor ou total aproximado quando houver métricas reais.
- **[Dados históricos heterogêneos]** Eventos atuais usam nomes e JSON variados → manter compatibilidade de leitura, catalogar códigos existentes e migrar escritores gradualmente.
- **[Redaction incompleta]** Um novo campo sensível pode entrar em `ChangesJson` → concentrar serialização em serviço único, cobrir campos proibidos com testes e revisar o catálogo em code review.
- **[Diferenças de provider]** Sintaxe e semântica de índices podem divergir → gerar migrations separadas e executar a matriz de provider antes do merge.
- **[Cache entre espaços]** Query cache pode exibir dados anteriores após troca de contexto → usar chave com tenant/espaço e invalidar/limpar cache na transição, seguindo o padrão de autenticação atual.

## Migration Plan

1. Adicionar abstração/catálogo de escrita e migrar os serviços existentes, sem alterar a tabela.
2. Adicionar contrato de consulta, controller, permissão e testes de isolamento.
3. Criar migrations de índices nos dois providers e validar aplicação repetida.
4. Publicar frontend com rota, navegação condicionada e modal de detalhes.
5. Validar build, testes unitários, integração, E2E e matriz PostgreSQL/SQL Server.
6. Implantar primeiro em ambiente de homologação, medir latência/volume e só então ativar a tela em produção.

Rollback de aplicação: retornar para o build anterior mantendo a tabela e os índices, pois são compatíveis. Não remover registros nem reverter migrations destrutivamente. Caso um índice cause impacto, removê-lo por nova migration corretiva após medição.

## Open Questions

Nenhuma questão aberta altera o contrato ou a abordagem. Retenção, exportação e campos adicionais de segurança podem ser definidos posteriormente como mudanças operacionais separadas.
