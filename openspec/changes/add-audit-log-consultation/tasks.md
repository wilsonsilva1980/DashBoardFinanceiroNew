# Tasks

## 1. Centralizar a escrita de auditoria

- [x] 1.1 Criar o contrato e a implementação do escritor central de auditoria, resolvendo ator, correlação e tenant/espaço pelo contexto do servidor, e verificar com testes unitários de escopo e usuário.
- [x] 1.2 Criar catálogo de ações/entidades e rótulos, preservando compatibilidade com os códigos já persistidos, e verificar ausência de códigos duplicados por teste.
- [x] 1.3 Implementar serialização, limite de tamanho, validação JSON e redaction de senha, token, cookie, segredo e identificadores bancários completos, e verificar os casos positivos/negativos com testes unitários.
- [x] 1.4 Migrar pagamentos, liquidações históricas, acordos, obrigações, onboarding e gestão de acessos para o escritor central sem mover o `SaveChanges`, e verificar que cada operação crítica mantém seu evento na mesma transação.

## 2. Consulta protegida no backend

- [x] 2.1 Criar contratos de filtro, página e resultado da auditoria com limites documentados para período e `pageSize`, e verificar validações de datas, UUIDs, ação, correlação e intervalo invertido.
- [x] 2.2 Implementar o caso de uso de consulta somente leitura com projeção, filtros por período/ação/entidade/entidade-id/usuário/correlação e ordenação `Instant DESC, Id DESC`, e verificar resultados com testes de integração.
- [x] 2.3 Criar `GET /api/v1/auditoria` e `GET /api/v1/auditoria/eventos` sob `auditoria.visualizar`, retornando página, total, ator e detalhes seguros, e verificar contratos OpenAPI e respostas HTTP.
- [x] 2.4 Adicionar índices equivalentes para tempo, ação, usuário, entidade/ID e correlação em migrations separadas de PostgreSQL e SQL Server, e verificar aplicação repetida sem duplicação.
- [x] 2.5 Adicionar testes de integração para 403 sem permissão, isolamento entre espaços/tenants, ausência de dados fora do escopo, paginação limitada e atomicidade de eventos críticos, e verificar a matriz de provider.

## 3. Tela de auditoria no frontend

- [x] 3.1 Adicionar contratos TypeScript, chave de query particionada por contexto e cliente da API de auditoria, e verificar compilação TypeScript.
- [x] 3.2 Criar a feature `/auditoria` com filtros, tabela paginada, loading, erro e lista vazia, seguindo os componentes compartilhados existentes, e verificar comportamento com testes de componente/unidade.
- [x] 3.3 Criar detalhe somente leitura em `Dialog` com timestamp, ação, entidade, ID, usuário, correlação e JSON escapado, e verificar que não há controles de edição/exclusão nem exposição de dados sensíveis.
- [x] 3.4 Condicionar navegação e rota à permissão `auditoria.visualizar`, limpar cache ao trocar espaço/sair e verificar o fluxo completo com teste Playwright.

## 4. Observabilidade e documentação

- [x] 4.1 Adicionar logs técnicos estruturados nos limites relevantes da API sem duplicar auditoria funcional nem registrar segredos, e verificar correlação e redaction por teste ou inspeção de sink.
- [x] 4.2 Documentar endpoint, filtros, catálogo de eventos, limites, retenção prevista e distinção entre auditoria e logs operacionais, e verificar que os exemplos correspondem ao contrato OpenAPI.
- [x] 4.3 Atualizar a documentação de verificação com os cenários de isolamento, autorização e migrations dos dois providers, e verificar revisão cruzada com a especificação `audit-trail`.

## 5. Verificação integrada

- [x] 5.1 Executar format check, build e testes backend sem banco e confirmar ausência de regressões nos testes existentes.
- [x] 5.2 Executar build e testes frontend, incluindo E2E da tela de auditoria e troca de espaço.
- [x] 5.3 Executar a matriz de migrations/integração PostgreSQL e SQL Server e confirmar que índices, constraints e consultas têm comportamento equivalente.
- [x] 5.4 Validar a mudança com `openspec validate --change add-audit-log-consultation --strict` e confirmar que todos os artefatos e tarefas estão prontos para revisão.

## Workflow follow-up

- Revisar os artefatos com a equipe antes de iniciar implementação.
- Quando aprovado, executar `$openspec-apply-change (Codex)` para implementar as tarefas.
- Após implementação e validação, arquivar a mudança pelo fluxo OpenSpec correspondente.
